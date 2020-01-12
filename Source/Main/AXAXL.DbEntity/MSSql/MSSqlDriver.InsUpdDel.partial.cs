using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Extensions;

namespace AXAXL.DbEntity.MSSql
{
	public partial class MSSqlDriver
	{
		public IEnumerable<dynamic> ExecuteCommand(string connectionString, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(string.IsNullOrEmpty(rawSqlCommand) == false, "Sql command is blank");
			try
			{
				var cmd = this.PrepareCommandFromRawSql(isStoredProcedure, rawSqlCommand, parameters, out IDictionary<string, SqlParameter> cmdParameters);

				this.LogSql("Execute command", null, cmd);
				IEnumerable<dynamic> result = null;
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					cmd.Connection = connection;
					using (var reader = cmd.ExecuteReader())
					{
						var idx2Name = reader.GetColumnSchema().ToDictionary(k => k.ColumnOrdinal.Value, v => v.ColumnName);
						result = reader
									.Cast<IDataRecord>()
									.Select(p =>
									{
										return idx2Name
												.ToDictionary(k => k.Value, v => p[v.Key])
												.ToDynamic();
									})
									.ToArray();
					}
				}
				outputParameters = cmdParameters
									.Where(kv => kv.Value.Direction != ParameterDirection.Input)
									.ToDictionary(
										k => k.Key,
										v => v.Value.SqlValue == DBNull.Value ? null : v.Value.Value
									);
				return result;
			}
			catch (SqlException ex)
			{
				throw new InvalidOperationException($"Failed to execute Raw Sql {rawSqlCommand}", ex);
			}
		}
		public IEnumerable<T> ExecuteCommand<T>(string connectionString, Node node, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(string.IsNullOrEmpty(rawSqlCommand) == false, "Sql command is blank");

			var cmd = this.PrepareCommandFromRawSql(isStoredProcedure, rawSqlCommand, parameters, out IDictionary<string, SqlParameter> cmdParameters);
			// Note that the SelectClause variable is not used.  We are just borrowing the call to create the data reader fetching func.
			var (SelectedColumns, DataReaderToEntityFunc) = this.sqlGenerator.CreateSelectComponent(string.Empty, node);
			this.LogSql("Execute command", null, cmd);

			var resultSet = this.ExecuteQuery<T>(connectionString, DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);

			outputParameters = cmdParameters
								.Where(kv => kv.Value.Direction != ParameterDirection.Input)
								.ToDictionary(
									k => k.Key,
									v => v.Value.SqlValue == DBNull.Value ? null : v.Value.Value
								);
			return resultSet;
		}

		public T Delete<T>(string connectionString, T entity, Node node) where T: class, ITrackable
		{
			var pKeyAndVersion = this.sqlGenerator.ExtractPrimaryKeyAndConcurrencyControlColumns(node);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, pKeyAndVersion);
			var whereParameter = this.sqlGenerator.CreateSqlParameters(node, pKeyAndVersion);
			var deleteClause = this.sqlGenerator.CreateDeleteClause(node);
			var valueReaders = this.sqlGenerator.CreatePropertyValueReaderMap(node, pKeyAndVersion);

			Debug.Assert(string.IsNullOrEmpty(whereClause) == false, $"Missing where clause for delete statement on entity '{node.NodeType.Name}'");

			var deleteSql = new SqlCommand(deleteClause + @" WHERE " + whereClause);
			var paramWithValues = 
				whereParameter
					.Select(p => {
						var reader = valueReaders[p.Key];
						var param = p.Value;
						var value = reader(entity);
						param.Value = value ?? DBNull.Value;
						return param;
					})
					.ToArray();
			deleteSql.Parameters.AddRange(paramWithValues);

			this.LogSql("Delete<T>", node, deleteSql);

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				deleteSql.Connection = connection;
				var returnCount = deleteSql.ExecuteNonQuery();

				if (returnCount <= 0)
				{
					throw new DbUpdateConcurrencyException(returnCount, deleteClause + whereClause, this.PrintParameters(paramWithValues));
				}
			}
			return entity;
		}

		public T Insert<T>(string connectionString, T entity, Node node) where T : class, ITrackable
		{
			var resultCount = 0;
			var tableName = this.sqlGenerator.FormatTableName(node);
			var outputComponent = this.sqlGenerator.CreateOutputComponent(node, true);
			var insertClauses = this.sqlGenerator.CreateInsertComponent(node);
			var insertParameters = this.sqlGenerator.CreateSqlParameters(node, insertClauses.InsertColumns);
			var propertyReader = this.sqlGenerator.CreatePropertyValueReaderMap(node, insertClauses.InsertColumns);
			var allActions = this.GetFrameworkUpdateActions(node, NodePropertyUpdateOptions.ByFwkOnInsert);
			var allFuncInj = this.CreateFrameworkUpdatesFromFuncInjection(node, NodePropertyUpdateOptions.ByFwkOnInsert);
			var allConstantColumns = node.AllDbColumns.Where(p => p.IsConstant).ToArray();

			foreach (var eachAction in allActions)
			{
				eachAction(entity);
			}
			foreach (var eachInj in allFuncInj)
			{
				eachInj.ActionOnProperty(entity, eachInj.FuncInjection());
			}
			foreach(var eachConstantCol in allConstantColumns)
			{
				eachConstantCol.ConstantValueSetterAction.Invoke(entity, eachConstantCol.ConstantValue);
			}

			var paramWithValues = insertParameters.Select(
					kv =>
					{
						var value = propertyReader[kv.Key](entity);
						var param = kv.Value;
						param.Value = value ?? DBNull.Value;
						return param;
					}
				).ToArray();

			var withNoOutput = string.IsNullOrEmpty(outputComponent.OutputClause) == true;
			var insertSql = string.Format(@"INSERT INTO {0} ({1}){2}VALUES ({3})", tableName, insertClauses.InsertColumnsClause, withNoOutput == false ? $" {outputComponent.OutputClause} " : string.Empty, insertClauses.InsertValueClause);
			var cmd = new SqlCommand(insertSql);
			cmd.Parameters.AddRange(paramWithValues);

			this.LogSql("Insert<T>", node, cmd);

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.Connection = connection;
				if (withNoOutput)
				{
					resultCount = cmd.ExecuteNonQuery();
				}
				else
				{
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							resultCount++;
							outputComponent.EntityUpdateAction(reader, entity);
						}
					}
				}
				if (resultCount != 1)
				{
					throw new InvalidOperationException($"{resultCount} returned from insert sql {insertSql} with parameters {this.PrintParameters(paramWithValues)}");
				}
			}
			return entity;
		}

		public T Update<T>(string connectionString, T entity, Node node) where T : class, ITrackable
		{
			var resultCount = 0;
			var tableName = this.sqlGenerator.FormatTableName(node);
			var pKeyAndVersion = this.sqlGenerator.ExtractPrimaryKeyAndConcurrencyControlColumns(node);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, pKeyAndVersion, @"upd_");
			var whereParameter = this.sqlGenerator.CreateSqlParameters(node, pKeyAndVersion, @"upd_");
			var whereReaders = this.sqlGenerator.CreatePropertyValueReaderMap(node, pKeyAndVersion);
			var outputComponent = this.sqlGenerator.CreateOutputComponent(node, false);
			var updateComponent = this.sqlGenerator.CreateUpdateAssignmentComponent(node);
			var updateParameters = this.sqlGenerator.CreateSqlParameters(node, updateComponent.UpdateColumns);
			var propertyReader = this.sqlGenerator.CreatePropertyValueReaderMap(node, updateComponent.UpdateColumns);
			var allActions = this.GetFrameworkUpdateActions(node, NodePropertyUpdateOptions.ByFwkOnUpdate);
			var allFuncInj = this.CreateFrameworkUpdatesFromFuncInjection(node, NodePropertyUpdateOptions.ByFwkOnUpdate);
			// capture current property value into sql parameters before updating property by framework injection
			var whereParameterWithValues =
				whereParameter
					.Select(p => {
						var reader = whereReaders[p.Key];
						var param = p.Value;
						var value = reader(entity);
						param.Value = value ?? DBNull.Value;
						return param;
					})
					.ToArray();
			foreach (var eachAction in allActions)
			{
				eachAction(entity);
			}
			foreach (var eachInj in allFuncInj)
			{
				eachInj.ActionOnProperty(entity, eachInj.FuncInjection());
			}
			var updateParmeterWithValues = updateParameters.Select(
					kv =>
					{
						var value = propertyReader[kv.Key](entity);
						var param = kv.Value;
						param.Value = value ?? DBNull.Value;
						return param;
					}
				).ToArray();
			var withNoOutput = string.IsNullOrEmpty(outputComponent.OutputClause) == true;
			var updateSql = string.Format(
				@"UPDATE {0} SET {1}{2} WHERE {3}", 
				tableName, 
				updateComponent.AssignmentClause, 
				withNoOutput ? string.Empty : $" {outputComponent.OutputClause} ", 
				whereClause
				);
			var cmd = new SqlCommand(updateSql);
			cmd.Parameters.AddRange(updateParmeterWithValues);
			cmd.Parameters.AddRange(whereParameterWithValues);

			this.LogSql("Update<T>", node, cmd);

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.Connection = connection;
				if (withNoOutput)
				{
					resultCount = cmd.ExecuteNonQuery();
				}
				else
				{
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							resultCount++;
							outputComponent.EntityUpdateAction(reader, entity);
						}
					}
				}
				if (resultCount != 1)
				{
					throw new DbUpdateConcurrencyException(resultCount, updateSql, this.PrintParameters(whereParameterWithValues));
				}
			}
			return entity;
		}
		public string GetSqlDbType(Type csType)
		{
			return this.sqlGenerator.GetSqlDbTypeFromCSType(csType).ToString();
		}
	}
}
