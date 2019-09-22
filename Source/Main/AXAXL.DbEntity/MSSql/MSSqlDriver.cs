using System;
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
	public class MSSqlDriver : IDatabaseDriver
	{
		private ILogger log = null;
		private IMSSqlGenerator sqlGenerator = null;
		private static readonly MethodInfo enumerableCastMethodInfo = typeof(Enumerable).GetMethod("Cast", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo compileConditionsMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(CompileConditions), BindingFlags.NonPublic | BindingFlags.Instance);
		public MSSqlDriver(ILoggerFactory factory, IMSSqlGenerator sqlGenerator)
		{
			this.log = factory.CreateLogger<MSSqlDriver>();
			this.sqlGenerator = sqlGenerator;
		}
		public IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(
				parameters.Keys.All(p => String.IsNullOrEmpty(node.GetDbColumnNameFromPropertyName(p)) == false),
				$"Dictionary contains key which is not present in the given node."
				);

			var select = this.sqlGenerator.CreateSelectComponent(@"t0", node, -1);
			var whereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var queryParameters = this.sqlGenerator.CreateSqlParameters(node, whereColumns);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, whereColumns);
			var cmd = new SqlCommand(
							select.SelectClause +
							(!string.IsNullOrEmpty(whereClause) ? @" WHERE " + whereClause : string.Empty)
							);

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

			this.LogSql("Select<T> with dictionary parameters", node, cmd);

			return this.ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
		}

		public IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters, IEnumerable<Expression> additionalWhereClauses, IEnumerable<Expression[]> additionalOrClauses, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(
				parameters.Keys.All(p => String.IsNullOrEmpty(node.GetDbColumnNameFromPropertyName(p)) == false),
				$"Dictionary contains key which is not present in the given node."
				);
			var tablePrefix = @"t";
			var tableAliasFirstIdx = 0;

			// The following prepare where clause and sql parameter from the dictionary of values.  Regular stuff.
			var select = this.sqlGenerator.CreateSelectComponent($"{tablePrefix}{tableAliasFirstIdx}", node, -1);
			var primaryWhereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var primaryQueryParameters = this.sqlGenerator.CreateSqlParameters(node, primaryWhereColumns);
			var primaryWhereClause = this.sqlGenerator.CreateWhereClause(node, primaryWhereColumns);

			// The following compile the additional where and or conditions.
			(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) topLevelJoin = (tableAliasFirstIdx, tableAliasFirstIdx, null);
			var innerJoinMap = new OrderedDictionary();
			innerJoinMap.Add("-", topLevelJoin);

			Type typeOfAdditionalWhereClauses;
			Type typeOfAdditionalOrClauses;

			var restoredWhereClauses = this.RestoreWhereClause(node, additionalWhereClauses, out typeOfAdditionalWhereClauses);
			var restoredOrClauses = this.RestoreOrClauses(node, additionalOrClauses, out typeOfAdditionalOrClauses);
			var compileConditionsDelegate = this.MakeCompileWithRightType(node, typeOfAdditionalWhereClauses, typeOfAdditionalOrClauses);

			var (additonalWhereStatements, additionalSqlParameters) = (ValueTuple<List<string>, List<Func<SqlParameter>>>)compileConditionsDelegate.DynamicInvoke(node, restoredWhereClauses, restoredOrClauses, tablePrefix, innerJoinMap);

			var additionalWhereClause = additonalWhereStatements.Count() > 0 ? @" AND " + string.Join(@" AND ", additonalWhereStatements) : string.Empty;
			var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);

			// Put them together to create the SQL command.
			var sqlCmd = string.Format("{0}{1} WHERE {2}{3}", select.SelectClause, innerJoinStatement, primaryWhereClause, additionalWhereClause);
			var cmd = new SqlCommand(sqlCmd);
			var parameterWithValue =
				primaryQueryParameters
					.Select(kv => {
						var whereParameterValue = parameters[kv.Key];
						var sqlParameter = kv.Value;
						sqlParameter.Value = whereParameterValue ?? DBNull.Value;
						return sqlParameter;
					})
					.ToArray();
			cmd.Parameters.AddRange(parameterWithValue);
			foreach (var parameter in additionalSqlParameters)
			{
				cmd.Parameters.Add(parameter.Invoke());
			}
			this.LogSql("Select<T> with where expression", node, cmd);
			return ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
		}

		public IEnumerable<T> Select<T>(string connectionString, Node node, IEnumerable<Expression<Func<T, bool>>> whereClauses, IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup, int maxNumOfRow, (NodeProperty Property, bool IsAscending)[] orderBy, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			var tablePrefix = @"t";
			var tableAliasFirstIdx = 0;

			// set the current node, which is the T as the starting point.  All other inner joins should be derived from this point upwards towards parent reference.
			(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) topLevelJoin = (tableAliasFirstIdx, tableAliasFirstIdx, null);
			var innerJoinMap = new OrderedDictionary();
			innerJoinMap.Add("-", topLevelJoin);

			var (whereStatements, sqlParameterList) = this.CompileConditions<T>(node, whereClauses, orClausesGroup, tablePrefix, innerJoinMap);

			var select = this.sqlGenerator.CreateSelectComponent($"{tablePrefix}{tableAliasFirstIdx}", node, maxNumOfRow);
			var orderByClause = this.sqlGenerator.CompileOrderByClause(orderBy);
			var whereStatement = whereStatements.Count() > 0 ? @" WHERE " + string.Join(@" AND ", whereStatements) : string.Empty;
			var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);
			var sqlCmd = select.SelectClause + innerJoinStatement + whereStatement + orderByClause;
			var cmd = new SqlCommand(sqlCmd);
			foreach (var parameter in sqlParameterList)
			{
				cmd.Parameters.Add(parameter.Invoke());
			}
			this.LogSql("Select<T> with where expression", node, cmd);
			return ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
		}
		/// <summary>
		/// Restore IEnumerable of Expression to IEnumerable of Expression&lt;Func&lt;T, bool&gt;&gt;.
		/// </summary>
		/// <param name="node">Type of this node will be the type of the delegate to be restored.</param>
		/// <param name="whereClauses">list of expression</param>
		/// <param name="restoredType">type of the original expression</param>
		/// <returns>Restored expression of the right type</returns>
		private object RestoreWhereClause(Node node, IEnumerable<Expression> whereClauses, out Type restoredType)
		{
			var originalWhereClauseType = typeof(Func<,>).MakeGenericType(node.NodeType, typeof(bool));
			var originalWhereExprType = typeof(Expression<>).MakeGenericType(originalWhereClauseType);
			restoredType = typeof(IEnumerable<>).MakeGenericType(originalWhereExprType);
			var typeCastDelegateType = typeof(Func<,>).MakeGenericType(whereClauses.GetType(), restoredType);
			var typeCastDeleage = enumerableCastMethodInfo
									.MakeGenericMethod(originalWhereExprType)
									.CreateDelegate(typeCastDelegateType);
			return typeCastDeleage.DynamicInvoke(whereClauses);
		}
		/// <summary>
		/// Restore IEnumerable of Expression[] to IEnumerable of Expression&lt;Func&lt;T, bool&gt;&gt;[].
		/// </summary>
		/// <param name="node">Type of this node will be the type of the delegate to be restored.</param>
		/// <param name="orClauses">list of expression[]</param>
		/// <param name="restoredType">type of the original expression</param>
		/// <returns>Restored expression of the right type</returns>
		private object RestoreOrClauses(Node node, IEnumerable<Expression[]> orClauses, out Type restoredType)
		{
			var originalWhereClauseType = typeof(Func<,>).MakeGenericType(node.NodeType, typeof(bool));
			var originalWhereExprType = typeof(Expression<>).MakeGenericType(originalWhereClauseType);
			var originalOrExprArrayType = originalWhereExprType.MakeArrayType();
			restoredType = typeof(IEnumerable<>).MakeGenericType(originalOrExprArrayType);
			var typeCastDelegateType = typeof(Func<,>).MakeGenericType(orClauses.GetType(), restoredType);
			var typeCastDeleage = enumerableCastMethodInfo
									.MakeGenericMethod(originalOrExprArrayType)
									.CreateDelegate(typeCastDelegateType);
			return typeCastDeleage.DynamicInvoke(orClauses);
		}
		/// <summary>
		/// Construct delegate of <seealso cref="CompileConditions{TEntity}(Node, IEnumerable{Expression{Func{TEntity, bool}}}, IEnumerable{Expression{Func{TEntity, bool}}[]}, string, OrderedDictionary)"/>
		/// with the right type for its generic type parameter.
		/// </summary>
		/// <param name="node">Node type will be used as the type parameter of the generic method <see cref="CompileConditions{TEntity}(Node, IEnumerable{Expression{Func{TEntity, bool}}}, IEnumerable{Expression{Func{TEntity, bool}}[]}, string, OrderedDictionary)"/></param>
		/// <param name="whereClausesType">type of the where clauses</param>
		/// <param name="orClausesType">type of the or clauses</param>
		/// <returns>delegate to call the CompileConditions method</returns>
		private Delegate MakeCompileWithRightType(Node node, Type whereClausesType, Type orClausesType)
		{
			var resultingValueTupleType = typeof(ValueTuple<List<string>, List<Func<SqlParameter>>>);
			var compileConditionsDelegateType = typeof(Func<,,,,,>)
													.MakeGenericType(
														typeof(Node), 
														whereClausesType, 
														orClausesType, 
														typeof(string), 
														typeof(OrderedDictionary),
														resultingValueTupleType
														);
			var delegateHandle = compileConditionsMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(compileConditionsDelegateType, this);
			return delegateHandle;
		}
		private
			(List<string> whereStatements, List<Func<SqlParameter>> sqlParameterList) 
			CompileConditions<TEntity>
		(
			Node node, 
			IEnumerable<Expression<Func<TEntity, bool>>> whereClauses, 
			IEnumerable<Expression<Func<TEntity, bool>>[]> orClausesGroup, 
			string tablePrefix, 
			OrderedDictionary innerJoinMap
		) where TEntity : class, new()
		{
			var sqlParameterRunningSeq = 0;
			var whereStatements = new List<string>();
			var sqlParameterList = new List<Func<SqlParameter>>();

			//var emptyWhere = (ParameterSequence: 0, WhereClause: string.Empty, InnerJoinsClause: string.Empty, SqlParameters: new Func<SqlParameter>[0]);

			foreach (var where in whereClauses)
			{
				var compilationResult = this.sqlGenerator
									.CompileWhereClause<TEntity>(
										node,
										sqlParameterRunningSeq,
										tablePrefix,
										innerJoinMap,
										where
										);
				sqlParameterRunningSeq = compilationResult.parameterSequence;
				whereStatements.Add(compilationResult.whereClause);
				sqlParameterList.AddRange(compilationResult.sqlParameters);
			}
			foreach (var orClauses in orClausesGroup)
			{
				List<string> orConditions = new List<string>();
				foreach (var or in orClauses)
				{
					var compilationResult = this.sqlGenerator
										.CompileWhereClause<TEntity>(
											node,
											sqlParameterRunningSeq,
											tablePrefix,
											innerJoinMap,
											or
											);
					sqlParameterRunningSeq = compilationResult.parameterSequence;
					orConditions.Add(compilationResult.whereClause);
					sqlParameterList.AddRange(compilationResult.sqlParameters);
				}
				if (orConditions.Count() > 0)
				{
					whereStatements.Add(@"( " + string.Join(" OR ", orConditions) + @" )");
				}
			}
			return (whereStatements, sqlParameterList);
		}

		public IEnumerable<dynamic> ExecuteCommand(string connectionString, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

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
								.Select(p => {
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
		public IEnumerable<T> ExecuteCommand<T>(string connectionString, Node node, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			var cmd = this.PrepareCommandFromRawSql(isStoredProcedure, rawSqlCommand, parameters, out IDictionary<string, SqlParameter> cmdParameters);
			// Note that the SelectClause variable is not used.  We are just borrowing the call to create the data reader fetching func.
			var (SelectClause, DataReaderToEntityFunc) = this.sqlGenerator.CreateSelectComponent(string.Empty, node, -1);
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
		internal static string FormatParameterName(string parameterName)
		{
			return parameterName.StartsWith('@') ? parameterName : "@" + parameterName;
		}

		private IEnumerable<T> ExecuteQuery<T>(string connectionString, Func<SqlDataReader, dynamic> fetcher, SqlCommand cmd, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			var resultSet = new List<T>();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				cmd.CommandTimeout = timeoutDurationInSeconds;
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
		private SqlCommand PrepareCommandFromRawSql(bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, SqlParameter> cmdParameters)
		{
			parameters = parameters ?? new (string, object, ParameterDirection)[0];
			cmdParameters = this.sqlGenerator.CreateSqlParametersForRawSqlParameters(parameters);
			foreach (var input in parameters)
			{
				if (cmdParameters.ContainsKey(input.Name))
				{
					cmdParameters[input.Name].Value = input.Value ?? DBNull.Value;
				}
			}
			var cmd = new SqlCommand(rawSqlCommand);
			cmd.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
			cmd.Parameters.AddRange(cmdParameters.Values.ToArray());

			return cmd;
		}
		private string PrintParameters(SqlParameter[] parameters)
		{
			var parameterValues = 
			parameters
				.Select(p => $"{p.ParameterName} = {p.Value}")
				.ToArray();

			return string.Join(", ", parameterValues);
		}

		protected virtual Action<dynamic>[] GetFrameworkUpdateActions(Node node, NodePropertyUpdateOptions currentOperation)
		{
			var actions = node.AllDbColumns
							.Where(c => (c.UpdateOption & currentOperation) != NodePropertyUpdateOptions.ByApp && c.ActionInjection != null)
							.Select(c => c.ActionInjection)
							.ToArray();
			return actions;
		}
		protected virtual (Action<dynamic, dynamic> ActionOnProperty, Func<dynamic> FuncInjection)[] CreateFrameworkUpdatesFromFuncInjection(Node node, NodePropertyUpdateOptions currentOperation)
		{
			var funcInjectColumns = node.AllDbColumns
										.Where(c => (c.UpdateOption & currentOperation) != NodePropertyUpdateOptions.ByApp && c.FuncInjection != null)
										.Select(
											c => (this.CreateFrameworkUpdateFromUncInjection(node, c), c.FuncInjection)
											)
										.ToArray()
										;
			return funcInjectColumns;
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

		private string ComputeInnerJoins(IOrderedDictionary innerJoinsMap, string tableAliasPrefix)
		{
			(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) eachEdge;
			StringBuilder buffer = new StringBuilder();
			var enumerator = innerJoinsMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				eachEdge = (ValueTuple<int, int, NodeEdge>)enumerator.Value;

				if (eachEdge.Edge == null) continue;

				var parentAlias = $"{tableAliasPrefix}{eachEdge.ParentTableAliasIdx}";
				var childAlias = $"{tableAliasPrefix}{eachEdge.ChildTableAliasIdx}";
				var nodeEdge = eachEdge.Edge;
				var parentTable = this.sqlGenerator.FormatTableName(nodeEdge.ParentNode, parentAlias);
				var childTable = this.sqlGenerator.FormatTableName(nodeEdge.ChildNode, childAlias);
				var parentKeys = nodeEdge.ParentNodePrimaryKeys;
				var childKeys = nodeEdge.ChildNodeForeignKeys;
				var keyIdx = 0;

				buffer
					.Append(@" INNER JOIN ")
					.Append(parentTable)
					.Append(@" ON ")
					;

				for (; keyIdx < parentKeys.Length; keyIdx++)
				{
					if (keyIdx > 0) buffer.Append(@" AND ");
					buffer.Append($"{parentAlias}.[{parentKeys[keyIdx].DbColumnName}] = {childAlias}.[{childKeys[keyIdx].DbColumnName}]");
				}
				for (; keyIdx < childKeys.Length; keyIdx++)
				{
					Debug.Assert(childKeys[keyIdx].IsConstant == true, $"There are more foreign keys than primary key on edge {nodeEdge.ParentNode.Name} -> {nodeEdge.ChildNode.Name}");
					buffer.Append(@" AND ");
					buffer.Append($"{childAlias}.[{childKeys[keyIdx].DbColumnName}] = {PrintConstantValueAsSqlCondition(childKeys[keyIdx])}");
				}
			}
			return buffer.ToString();
		}
		private static string PrintConstantValueAsSqlCondition(NodeProperty property)
		{
			Debug.Assert(property.IsConstant);
			var constantValue = property.ConstantValue;
			var propertyType = property.PropertyType;
			string formattedValue = null;

			if (propertyType.IsValueType)
			{
				if (propertyType.IsAssignableFrom(typeof(DateTime)))
				{
					var date = (DateTime)constantValue;
					formattedValue = $"'{date.ToString(QueryExtensionForSqlOperators.C_DATE_FORMAT_FOR_SQL)}'";
				}
				else
				{
					formattedValue = constantValue.ToString();
				}
			}
			else
			{
				formattedValue = $"'{constantValue.ToString()}'";
			}
			return formattedValue;
		}
		[Conditional("DEBUG")]
		private void LogSql(string message, Node node, SqlCommand cmd)
		{
			Debug.Assert(cmd != null);

			var sql = cmd.CommandText;
			var parameters = string.Join(
						", ",
						cmd.Parameters?
								.Cast<SqlParameter>()
								.Select(p =>
								{
									var name = p.ParameterName;
									var value = p.Value?.ToString();
									value = string.IsNullOrEmpty(value) ? @"null" : value;
									return $"{name} = {value}";
								})
						);
			this.log.LogDebug("| {0} | {1} | {2} | {3} |", message, node?.Name, sql, parameters);
		}
	}
}