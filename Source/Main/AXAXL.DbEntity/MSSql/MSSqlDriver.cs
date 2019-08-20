using System;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Extensions;

namespace AXAXL.DbEntity.MSSql
{
	public class MSSqlDriver : IDatabaseDriver
	{
		private ILogger log = null;
		private IMSSqlGenerator sqlGenerator = null;
		public MSSqlDriver(ILoggerFactory factory, IMSSqlGenerator sqlGenerator)
		{
			this.log = factory.CreateLogger<MSSqlDriver>();
			this.sqlGenerator = sqlGenerator;
		}
		public IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(
				parameters.Keys.All(p => String.IsNullOrEmpty(node.GetDbColumnNameFromPropertyName(p)) == false),
				$"Dictionary contains key which is not present in the given node."
				);

			var select = this.sqlGenerator.CreateSelectComponent(node);
			var whereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var queryParameters = this.sqlGenerator.CreateSqlParameters(node, whereColumns);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, whereColumns);
			var cmd = new SqlCommand(select.SelectClause + whereClause);

			var parameterWithValue = 
				queryParameters
					.Select(kv => {
						var whereParameterValue = parameters[kv.Key];
						var sqlParameter = kv.Value;
						sqlParameter.Value = whereParameterValue ?? DBNull.Value;
						return sqlParameter;
					})
					.ToArray();
			cmd.Parameters.AddRange(parameterWithValue);

			return this.ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd);
		}
		public IEnumerable<T> Select<T>(string connectionString, Node node, Expression<Func<T, bool>> whereClause) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			var where = this.sqlGenerator.CompileWhereClause<T>(node, whereClause);
			var select = this.sqlGenerator.CreateSelectComponent(node);
			var cmd = new SqlCommand(select.SelectClause + where.WhereClause);
			foreach (var parameter in where.SqlParameters)
			{
				cmd.Parameters.Add(parameter());
			}
			return ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd);
		}

		public IEnumerable<dynamic> Select(string connectionString, string rawQuery, IDictionary<string, object> parameters)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			var queryParameters = this.sqlGenerator.CreateSqlParametersForRawSqlParameters(parameters);

			IEnumerable<dynamic> result = null;
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var cmd = new SqlCommand(rawQuery, connection);
				if (queryParameters != null && queryParameters.Length > 0)
				{
					cmd.Parameters.AddRange(queryParameters);
				}

				using (var reader = cmd.ExecuteReader())
				{
					var idx2Name = reader.GetColumnSchema().ToDictionary(k => k.ColumnOrdinal.Value, v => v.ColumnName);
					result = reader
								.Cast<IDataRecord>()
								.Select(p => {
									return idx2Name
											.ToDictionary(k => k.Value, v => p[v.Key])
											.ToDynamic();
								})
								.ToArray();
				}
			}
			return result;
		}

		public T Delete<T>(string connectionString, T entity, Node node) where T: class, ITrackable, new()
		{
			var pKeyAndVersion = this.sqlGenerator.ExtractPrimaryKeyAndConcurrencyControlColumns(node);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, pKeyAndVersion);
			var whereParameter = this.sqlGenerator.CreateSqlParameters(node, pKeyAndVersion);
			var deleteClause = this.sqlGenerator.CreateDeleteClause(node);
			var valueReaders = this.sqlGenerator.CreatePropertyValueReaderMap(node, pKeyAndVersion);

			Debug.Assert(string.IsNullOrEmpty(whereClause) == false, $"Missing where clause for delete statement on entity '{node.NodeType.Name}'");

			var deleteSql = new SqlCommand(deleteClause + whereClause);
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
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				deleteSql.Connection = connection;
				var returnCount = deleteSql.ExecuteNonQuery();

				if (returnCount <= 0)
				{
					throw new InvalidOperationException($"{returnCount} count returned by {deleteClause+whereClause} with parameters {this.PrintParameters(paramWithValues)}");
				}
			}
			return entity;
		}

		public T Insert<T>(string connectionString, T entity, Node node) where T : class, ITrackable, new()
		{
			var resultCount = 0;
			var tableName = this.sqlGenerator.FormatTableName(node);
			var outputComponent = this.sqlGenerator.CreateOutputComponent(node, true);
			var insertClauses = this.sqlGenerator.CreateInsertComponent(node);
			var insertParameters = this.sqlGenerator.CreateSqlParameters(node, insertClauses.InsertColumns);
			var propertyReader = this.sqlGenerator.CreatePropertyValueReaderMap(node, insertClauses.InsertColumns);
			var allActions = this.GetFrameworkUpdateActions(node, NodePropertyUpdateOptions.ByFwkOnInsert, NodePropertyUpdateOptions.ByDbOnInsertAndUpdate);
			var allFuncInj = this.CreateFrameworkUpdatesFromFuncInjection(node, NodePropertyUpdateOptions.ByFwkOnInsert, NodePropertyUpdateOptions.ByDbOnInsertAndUpdate);
			// capture current property value into sql parameters before updating property by framework injection
			var paramWithValues = insertParameters.Select(
					kv =>
					{
						var value = propertyReader[kv.Key](entity);
						var param = kv.Value;
						param.Value = value ?? DBNull.Value;
						return param;
					}
				).ToArray();
			foreach (var eachAction in allActions)
			{
				eachAction(entity);
			}
			foreach(var eachInj in allFuncInj)
			{
				eachInj.ActionOnProperty(entity, eachInj.FuncInjection());
			}
			var withOutput = string.IsNullOrEmpty(outputComponent.OutputClause) == false;
			var insertSql = string.Format(@"INSERT INTO {0} ({1}){2}VALUES ({3})", tableName, insertClauses.InsertColumnsClause, withOutput ? $" {outputComponent.OutputClause} " : string.Empty, insertClauses.InsertValueClause);
			var cmd = new SqlCommand(insertSql);
			cmd.Parameters.AddRange(paramWithValues);
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.Connection = connection;
				if (withOutput)
				{
					resultCount = cmd.ExecuteNonQuery();
				}
				else
				{
					using (var reader = cmd.ExecuteReader())
					{
						resultCount++;
						outputComponent.EntityUpdateAction(reader, entity);
					}
				}
				if (resultCount != 1)
				{
					throw new InvalidOperationException($"{resultCount} returned from insert sql {insertSql} with parameters {this.PrintParameters(paramWithValues)}");
				}
			}
			return entity;
		}

		public T Update<T>(string connectionString, T entity, Node node) where T : class, ITrackable, new()
		{
			var resultCount = 0;
			var tableName = this.sqlGenerator.FormatTableName(node);
			var pKeyAndVersion = this.sqlGenerator.ExtractPrimaryKeyAndConcurrencyControlColumns(node);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, pKeyAndVersion, @"upd_");
			var whereParameter = this.sqlGenerator.CreateSqlParameters(node, pKeyAndVersion, @"upd_");
			var whereReaders = this.sqlGenerator.CreatePropertyValueReaderMap(node, pKeyAndVersion);
			var outputComponent = this.sqlGenerator.CreateOutputComponent(node, true);
			var updateComponent = this.sqlGenerator.CreateUpdateAssignmentComponent(node);
			var updateParameters = this.sqlGenerator.CreateSqlParameters(node, updateComponent.UpdateColumns);
			var propertyReader = this.sqlGenerator.CreatePropertyValueReaderMap(node, updateComponent.UpdateColumns);
			var allActions = this.GetFrameworkUpdateActions(node, NodePropertyUpdateOptions.ByFwkOnUpdate, NodePropertyUpdateOptions.ByDbOnInsertAndUpdate);
			var allFuncInj = this.CreateFrameworkUpdatesFromFuncInjection(node, NodePropertyUpdateOptions.ByFwkOnUpdate, NodePropertyUpdateOptions.ByDbOnInsertAndUpdate);
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
			var updateParmeterWithValues = updateParameters.Select(
					kv =>
					{
						var value = propertyReader[kv.Key](entity);
						var param = kv.Value;
						param.Value = value ?? DBNull.Value;
						return param;
					}
				).ToArray();
			foreach (var eachAction in allActions)
			{
				eachAction(entity);
			}
			foreach (var eachInj in allFuncInj)
			{
				eachInj.ActionOnProperty(entity, eachInj.FuncInjection());
			}
			var withOutput = string.IsNullOrEmpty(outputComponent.OutputClause) == false;
			var updateSql = string.Format(@"UPDATE {0} SET {1}{2} {3}", tableName, updateComponent.AssignmentClause, withOutput ? $" {outputComponent.OutputClause} " : string.Empty, whereClause);
			var cmd = new SqlCommand(updateSql);
			cmd.Parameters.AddRange(updateParmeterWithValues);
			cmd.Parameters.AddRange(whereParameterWithValues);

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.Connection = connection;
				if (withOutput)
				{
					resultCount = cmd.ExecuteNonQuery();
				}
				else
				{
					using (var reader = cmd.ExecuteReader())
					{
						resultCount++;
						outputComponent.EntityUpdateAction(reader, entity);
					}
				}
				if (resultCount != 1)
				{
					throw new InvalidOperationException($"{resultCount} returned from update sql {updateSql} with parameter for where clause is {this.PrintParameters(whereParameterWithValues)}");
				}
			}
			return entity;
		}
		public string GetSqlDbType(Type csType)
		{
			return this.sqlGenerator.GetSqlDbTypeFromCSType(csType).ToString();
		}
		internal static string FormatParameterName(string parameterName)
		{
			return parameterName.StartsWith('@') ? parameterName : "@" + parameterName;
		}

		private IEnumerable<T> ExecuteQuery<T>(string connectionString, Func<SqlDataReader, dynamic> fetcher, SqlCommand cmd) where T : class, new()
		{
			var resultSet = new List<T>();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.Connection = connection;
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var entity = fetcher(reader);
						resultSet.Add(entity);
					}
				}
			}
			return resultSet;
		}

		private string PrintParameters(SqlParameter[] parameters)
		{
			var parameterValues = 
			parameters
				.Select(p => $"{p.ParameterName} = {p.Value}")
				.ToArray();

			return string.Join(", ", parameterValues);
		}

		protected virtual Action<dynamic>[] GetFrameworkUpdateActions(Node node, params NodePropertyUpdateOptions[] updateOption)
		{
			var actions = this.GetAllColumns(node)
							.Where(c => updateOption.Contains(c.UpdateOption) && c.ActionInjection != null)
							.Select(c => c.ActionInjection)
							.ToArray();
			return actions;
		}
		protected virtual (Action<dynamic, dynamic> ActionOnProperty, Func<dynamic> FuncInjection)[] CreateFrameworkUpdatesFromFuncInjection(Node node, params NodePropertyUpdateOptions[] updateOption)
		{
			var funcInjectColumns = this.GetAllColumns(node)
										.Where(c => updateOption.Contains(c.UpdateOption) && c.FuncInjection != null)
										.Select(
											c => (this.CreateFrameworkUpdateFromUncInjection(node, c), c.FuncInjection)
											)
										.ToArray()
										;
			return funcInjectColumns;
		}
		private IList<NodeProperty> GetAllColumns(Node node)
		{
			var allColumns = node.PrimaryKeys.Values
								.Concat(
									node.DataColumns.Values.Where(p => string.IsNullOrEmpty(p.DbColumnName))
								)
								.ToList();
			if (node.ConcurrencyControl != null)
			{
				allColumns.Add(node.ConcurrencyControl);
			}
			return allColumns;
		}
		private Action<dynamic, dynamic> CreateFrameworkUpdateFromUncInjection(Node node, NodeProperty property)
		{
			var entityInput = Expression.Parameter(typeof(object), "entity");
			var valueInput = Expression.Parameter(typeof(object), "value");
			var assignment = Expression.Assign(
				Expression.Property(
					Expression.Convert(entityInput, node.NodeType),
					property.PropertyName
					),
				Expression.Convert(valueInput, property.PropertyType)
			);
			var block = Expression.Block(new [] { assignment });
			var lambda = Expression.Lambda<Action<dynamic, dynamic>>(block, new [] { entityInput, valueInput });

			return lambda.Compile();
		}
	}
}