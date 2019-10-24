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
	internal partial class MSSqlDriver : IDatabaseDriver
	{
		private readonly ILogger log = null;
		private readonly IMSSqlGenerator sqlGenerator = null;
		private static readonly MethodInfo enumerableCastMethodInfo = typeof(Enumerable).GetMethod("Cast", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo compileWhereConditionsMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(CompileWhereConditions), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo compileOrGroupsMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(CompileOrGroups), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo selectImplementationMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(SelectImplementation), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly IDictionary<string, object> emptyParameters = new Dictionary<string, object>();
		private static readonly IEnumerable<ValueTuple<NodeEdge, Expression>> emptyValueTupleOfNodeEdgeNExpression = Array.Empty<(NodeEdge, Expression)>();
		private static readonly IEnumerable<ValueTuple<NodeEdge, Expression[]>> emptyValueTupleOfNodeEdgeNExpressionGroup = Array.Empty<(NodeEdge, Expression[])>();
		private static readonly ValueTuple<NodeProperty, bool>[] emptyOrderBy = Array.Empty<(NodeProperty, bool)>();
		public MSSqlDriver(ILoggerFactory factory, IMSSqlGenerator sqlGenerator)
		{
			this.log = factory.CreateLogger<MSSqlDriver>();
			this.sqlGenerator = sqlGenerator;
		}

		[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL generated is using SQL parameters to pass user input.")]
		public IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters, int timeoutDurationInSeconds = 30) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
			Debug.Assert(parameters != null && parameters.Count > 0);
			Debug.Assert(
				parameters.Keys.All(p => String.IsNullOrEmpty(node.GetDbColumnNameFromPropertyName(p)) == false),
				$"Dictionary contains key which is not present in the given node."
				);

			var aliasT0 = @"t0";
			var select = this.sqlGenerator.CreateSelectComponent(@"t0", node, -1);
			var whereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var queryParameters = this.sqlGenerator.CreateSqlParameters(node, whereColumns);
			var whereClause = this.sqlGenerator.CreateWhereClause(node, whereColumns, tableAlias: aliasT0);
			SqlCommand cmd = new SqlCommand(
							select.SelectClause +
							(!string.IsNullOrEmpty(whereClause) ? @" WHERE " + whereClause : string.Empty)
							);

			var parameterWithValue =
				queryParameters
					.Select(kv =>
					{
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

		public IEnumerable<T> Select<T>(
			string connectionString, 
			Node node, 
			IDictionary<string, object> parameters, 
			IEnumerable<Expression> additionalWhereClauses, 
			IEnumerable<Expression[]> additionalOrClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression>> childInnerJoinWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childInnerJoinOrClausesGroup,
			int timeoutDurationInSeconds = 30
			) where T : class, new()
		{
			Debug.Assert(parameters != null && parameters.Count > 0);

			Type typeOfAdditionalWhereClauses;
			Type typeOfAdditionalOrClauses;
			var restoredWhereClauses = this.RestoreWhereClause(node, additionalWhereClauses, out typeOfAdditionalWhereClauses);
			var restoredOrClauses = this.RestoreOrClauses(node, additionalOrClauses, out typeOfAdditionalOrClauses);
			var enumerableOfTType = typeof(IEnumerable<>).MakeGenericType(node.NodeType);
			var selectDelegateType = typeof(Func<,,,,,,,,,,>)
													.MakeGenericType(
														typeof(string),
														typeof(Node),
														typeof(IDictionary<string, object>),
														typeOfAdditionalWhereClauses,
														typeOfAdditionalOrClauses,
														typeof(IEnumerable<ValueTuple<NodeEdge, Expression>>),
														typeof(IEnumerable<ValueTuple<NodeEdge, Expression[]>>),
														typeof(int),
														typeof(ValueTuple<NodeProperty, bool>[]),
														typeof(int),
														enumerableOfTType
														);
			var delegateHandle = selectImplementationMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(selectDelegateType, this);
			return (IEnumerable<T>)delegateHandle.DynamicInvoke(
				connectionString,
				node,
				parameters,
				restoredWhereClauses,
				restoredOrClauses,
				childInnerJoinWhereClauses,
				childInnerJoinOrClausesGroup,
				-1,
				MSSqlDriver.emptyOrderBy,
				timeoutDurationInSeconds
				);
		}

		public IEnumerable<T> Select<T>(
			string connectionString,
			Node node,
			IEnumerable<Expression<Func<T, bool>>> whereClauses,
			IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
			IEnumerable<ValueTuple<NodeEdge, Expression>> childInnerJoinWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childInnerJoinOrClausesGroup,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy,
			int timeoutDurationInSeconds = 30
			) where T : class, new()
		{
			return this.SelectImplementation<T>(connectionString, node, MSSqlDriver.emptyParameters, whereClauses, orClausesGroup, childInnerJoinWhereClauses, childInnerJoinOrClausesGroup, maxNumOfRow, orderBy, timeoutDurationInSeconds);
		}

		[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL generated are using SQL parameters for user input.")]
		private IEnumerable<T> SelectImplementation<T>(
			string connectionString,
			Node node,
			IDictionary<string, object> parameters,
			IEnumerable<Expression<Func<T, bool>>> whereClauses,
			IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
			IEnumerable<ValueTuple<NodeEdge, Expression>> childInnerJoinWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childInnerJoinOrClausesGroup,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy,
			int timeoutDurationInSeconds = 30
			) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			IEnumerable<T> resultSet = null;
			var tablePrefix = @"t";
			var tableAliasFirstIdx = 0;
			int sqlParameterRunningSeq = 0;
			var topLevelTableAlias = $"{tablePrefix}{tableAliasFirstIdx}";

			var select = this.sqlGenerator.CreateSelectComponent(topLevelTableAlias, node, maxNumOfRow);

			var primaryWhereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var primaryQueryParameters = this.sqlGenerator.CreateSqlParameters(node, primaryWhereColumns);
			var primaryWhereStatement = this.sqlGenerator.CreateWhereClause(node, primaryWhereColumns, tableAlias: topLevelTableAlias);
			var orderByClause = this.sqlGenerator.CompileOrderByClause(orderBy, topLevelTableAlias);

			// set the current node, which is the T as the starting point.  All other inner joins should be derived from this point upwards towards parent reference.
			var innerJoinMap = new InnerJoinMap();
			var rootMapKey = innerJoinMap.Init(node, tableAliasFirstIdx);
			var (additionalWhereStatements, additionalWhereSqlParameters) = this.CompileWhereConditions<T>(node, whereClauses, tablePrefix, sqlParameterRunningSeq, innerJoinMap);
			var (additionalOrStatements, additionalOrSqlParameters) = this.CompileOrGroups<T>(node, orClausesGroup, tablePrefix, sqlParameterRunningSeq, innerJoinMap);
			var (innerJoinWhereStatements, innerJoinSqlParameters) = this.CompileInnerJoinWhere(node, rootMapKey, childInnerJoinWhereClauses, tablePrefix, innerJoinMap);
			var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);

			var whereStatements = this.CombineWhereStatements(true, primaryWhereStatement, additionalWhereStatements, additionalOrStatements);
			var sqlCmd = string.Format("{0}{1}{2}{3}", select.SelectClause, innerJoinStatement, whereStatements, orderByClause);
			using (SqlCommand cmd = new SqlCommand(sqlCmd))
			{
				var primaryParameterWithValue =
						primaryQueryParameters
							.Select(kv =>
							{
								var whereParameterValue = parameters[kv.Key];
								var sqlParameter = kv.Value;
								sqlParameter.Value = whereParameterValue ?? DBNull.Value;
								return sqlParameter;
							})
							.ToArray();
				cmd.Parameters.AddRange(primaryParameterWithValue);
				foreach (var parameter in additionalWhereSqlParameters)
				{
					cmd.Parameters.Add(parameter.Invoke());
				}
				foreach (var parameter in additionalOrSqlParameters)
				{
					cmd.Parameters.Add(parameter.Invoke());
				}
				this.LogSql("Select<T> with where expression", node, cmd);
				resultSet = ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
			}
			return resultSet;
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
		/// Construct delegate of <seealso cref="CompileWhereConditions"/>
		/// with the right type for its generic type parameter.
		/// </summary>
		/// <param name="node">Node type will be used as the type parameter of the generic method <see cref="CompileConditions{TEntity}(Node, IEnumerable{Expression{Func{TEntity, bool}}}, IEnumerable{Expression{Func{TEntity, bool}}[]}, string, OrderedDictionary)"/></param>
		/// <param name="whereClausesType">type of the where clauses</param>
		/// <returns>delegate to call the CompileConditions method</returns>
		private Delegate MakeCompileWhereWithRightType(Node node, Type whereClausesType)
		{
			var resultingValueTupleType = typeof(ValueTuple<List<string>, List<Func<SqlParameter>>>);
			var compileConditionsDelegateType = typeof(Func<,,,,>)
													.MakeGenericType(
														typeof(Node), 
														whereClausesType, 
														typeof(string), 
														typeof(IInnerJoinMap),
														resultingValueTupleType
														);
			var delegateHandle = compileWhereConditionsMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(compileConditionsDelegateType, this);
			return delegateHandle;
		}

		private Delegate MakeCompileOrWithRightType(Node node, Type orClausesType)
		{
			var resultingValueTupleType = typeof(ValueTuple<List<string>, List<Func<SqlParameter>>>);
			var compileOrGroupDelegateType = typeof(Func<,,,,>)
													.MakeGenericType(
														typeof(Node),
														orClausesType,
														typeof(string),
														typeof(IInnerJoinMap),
														resultingValueTupleType
														);
			var delegateHandle = compileOrGroupsMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(compileOrGroupDelegateType, this);
			return delegateHandle;
		}
		private (string, List<Func<SqlParameter>>) CompileWhereConditions<TEntity>
		(
			Node node,
			IEnumerable<Expression<Func<TEntity, bool>>> whereClauses,
			string tablePrefix,
			int sqlParameterRunningSeq,
			IInnerJoinMap innerJoinMap
		) where TEntity : class, new()
		{
			//var sqlParameterRunningSeq = 0;
			var whereStatements = new List<string>();
			var sqlParameterList = new List<Func<SqlParameter>>();

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
			return (string.Join(@" AND ", whereStatements), sqlParameterList);
		}

		private	(string, List<Func<SqlParameter>>) CompileOrGroups<TEntity>
		(
			Node node, 
			IEnumerable<Expression<Func<TEntity, bool>>[]> orClausesGroup, 
			string tablePrefix,
			int sqlParameterRunningSeq,
			IInnerJoinMap innerJoinMap
		) where TEntity : class, new()
		{
			//var sqlParameterRunningSeq = 0;
			var whereStatements = new List<string>();
			var sqlParameterList = new List<Func<SqlParameter>>();

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
			return (string.Join(@" AND ", whereStatements), sqlParameterList);
		}
		private (string, List<Func<SqlParameter>>) CompileInnerJoinWhere(Node node, string currentMapKey, IEnumerable<ValueTuple<NodeEdge, Expression>> innerJoinToChildren, string tablePrefix, IInnerJoinMap innerJoinMap)
		{
			List<string> resultedWhereStatements = new List<string>();
			List<Func<SqlParameter>> resultedSqlParameters = new List<Func<SqlParameter>>();

			var innerJoinsFound = this.SearchInnerJoins<Expression>(node, currentMapKey, innerJoinToChildren.ToList(), innerJoinMap);
			foreach(var eachChildGroup in innerJoinsFound.GroupBy(i => i.Edge.ChildNode))
			{
				Type restoredType;
				var expressionsOnChild = eachChildGroup.Select(v => v.Expr).ToArray();
				var restoredWhere = this.RestoreWhereClause(eachChildGroup.Key, expressionsOnChild, out restoredType);
				var compileDelegate = this.MakeCompileWhereWithRightType(eachChildGroup.Key, restoredType);
				var compilationResult = ((string, List<Func<SqlParameter>>))compileDelegate.DynamicInvoke(eachChildGroup.Key, restoredWhere, tablePrefix, innerJoinMap);
				resultedWhereStatements.Add(compilationResult.Item1);
				resultedSqlParameters.AddRange(compilationResult.Item2);
			}
			var combined = this.CombineWhereStatements(false, resultedWhereStatements.ToArray());
			return (combined, resultedSqlParameters);
		}
		private (string, List<Func<SqlParameter>>) CompileInnerJoinOrGroup(Node node, string currentMapKey, IEnumerable<ValueTuple<NodeEdge, Expression[]>> innerJoinToChildren, string tablePrefix, IInnerJoinMap innerJoinMap)
		{
			List<string> resultedOrGroups = new List<string>();
			List<Func<SqlParameter>> resultedSqlParameters = new List<Func<SqlParameter>>();

			var innerJoinsFound = this.SearchInnerJoins<Expression[]>(node, currentMapKey, innerJoinToChildren.ToList(), innerJoinMap);
			foreach (var eachChildGroup in innerJoinsFound.GroupBy(i => i.Edge.ChildNode))
			{
				Type restoredType;
				var expressionsOnChild = eachChildGroup.Select(v => v.Expr).ToArray();
				var restoredOrGroups = this.RestoreOrClauses(eachChildGroup.Key, expressionsOnChild, out restoredType);
				var compileDelegate = this.MakeCompileOrWithRightType(eachChildGroup.Key, restoredType);
				var compilationResult = ((string, List<Func<SqlParameter>>))compileDelegate.DynamicInvoke(eachChildGroup.Key, restoredOrGroups, tablePrefix, innerJoinMap);
				resultedOrGroups.Add(compilationResult.Item1);
				resultedSqlParameters.AddRange(compilationResult.Item2);
			}
			var combined = this.CombineWhereStatements(false, resultedOrGroups.ToArray());
			return (combined, resultedSqlParameters);
		}
		private IEnumerable<(NodeEdge Edge, T Expr)> SearchInnerJoins<T>(Node node, string currentMapKey, List<ValueTuple<NodeEdge, T>> innerJoinToChildren, IInnerJoinMap innerJoinMap)
		{
			List<(NodeEdge Edge, T Expr)> searchResult = new List<(NodeEdge Edge, T Expr)>();
			this.SearchChildEdge<T>(node, innerJoinToChildren, searchResult);
			foreach(var result in searchResult)
			{
				innerJoinMap.Add(currentMapKey, result.Edge.ParentNode, result.Edge.ChildReferenceOnParentNode.PropertyName);
			}
			return searchResult;
		}

		private void SearchChildEdge<T>(Node node, List<ValueTuple<NodeEdge, T>> innerJoinToChildren, IInnerJoinMap innerJoinMap)
		{
			for(int i = innerJoinToChildren.Count - 1; i >= 0; i--)
			{
				var innerJoin = innerJoinToChildren[i];
				var path = new Stack<string>();
				var found = this.DepthFirstSearchChildEdge(node, innerJoin.Item1.ParentNode, path);
				Debug.Assert(found, $"Failed to locate {innerJoin.Item1.ParentNode.Name}");
				string mapKey = null;
				foreach(var eachPath in path.ToArray().Reverse())
				{

				}
			}
		}
		private IGrouping<>
		private bool DepthFirstSearchChildEdge(Node node, Node targetNode, Stack<NodeEdge> pathFromNodeToTarget)
		{
			bool found = true;
			if (targetNode.Name != node.Name)
			{
				foreach(var edge in node.AllChildEdges())
				{
					pathFromNodeToTarget.Push(edge);
					found = this.DepthFirstSearchChildEdge(edge.ChildNode, targetNode, pathFromNodeToTarget);

					if (found)
					{
						break;
					}
					else
					{
						pathFromNodeToTarget.Pop();
					}
				}
			}
			return found;
		}
		private string CombineWhereStatements(bool addWhere, params string[] whereStatements)
		{
			if (whereStatements == null || whereStatements.Length <= 0) return string.Empty;
			var combined = string.Join(@" AND ", whereStatements.Where(s => string.IsNullOrEmpty(s) == false));
			return string.IsNullOrEmpty(combined) ? string.Empty : $" WHERE {combined}";
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

		private string ComputeInnerJoins(IInnerJoinMap innerJoinsMap, string tableAliasPrefix)
		{
			StringBuilder buffer = new StringBuilder();
			foreach (var eachEdge in innerJoinsMap.Joins)
			{
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

		#region archived at 2019-10-22 by P. Sham.
		//public IEnumerable<T> Select<T>(
		//	string connectionString,
		//	Node node,
		//	IDictionary<string, object> parameters,
		//	IEnumerable<Expression> additionalWhereClauses,
		//	IEnumerable<Expression[]> additionalOrClauses,
		//	IEnumerable<ValueTuple<NodeEdge, Expression>> childInnerJoinWhereClauses,
		//	IEnumerable<ValueTuple<NodeEdge, Expression[]>> childInnerJoinOrClausesGroup,
		//	int timeoutDurationInSeconds = 30
		//	) where T : class, new()
		//{
		//	Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");
		//	Debug.Assert(
		//		parameters.Keys.All(p => String.IsNullOrEmpty(node.GetDbColumnNameFromPropertyName(p)) == false),
		//		$"Dictionary contains key which is not present in the given node."
		//		);
		//	var tablePrefix = @"t";
		//	var tableAliasFirstIdx = 0;

		//	// The following prepare where clause and sql parameter from the dictionary of values.  Regular stuff.
		//	var select = this.sqlGenerator.CreateSelectComponent($"{tablePrefix}{tableAliasFirstIdx}", node, -1);
		//	var primaryWhereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
		//	var primaryQueryParameters = this.sqlGenerator.CreateSqlParameters(node, primaryWhereColumns);
		//	var primaryWhereClause = this.sqlGenerator.CreateWhereClause(node, primaryWhereColumns);

		//	// The following compile the additional where and or conditions.
		//	var innerJoinMap = new InnerJoinMap();
		//	innerJoinMap.Init(node, tableAliasFirstIdx);

		//	Type typeOfAdditionalWhereClauses;
		//	Type typeOfAdditionalOrClauses;

		//	var restoredWhereClauses = this.RestoreWhereClause(node, additionalWhereClauses, out typeOfAdditionalWhereClauses);
		//	var restoredOrClauses = this.RestoreOrClauses(node, additionalOrClauses, out typeOfAdditionalOrClauses);
		//	var compileConditionsDelegate = this.MakeCompileWithRightType(node, typeOfAdditionalWhereClauses, typeOfAdditionalOrClauses);

		//	var (additonalWhereStatements, additionalSqlParameters) = (ValueTuple<List<string>, List<Func<SqlParameter>>>)compileConditionsDelegate.DynamicInvoke(node, restoredWhereClauses, restoredOrClauses, tablePrefix, innerJoinMap);

		//	var additionalWhereClause = additonalWhereStatements.Count() > 0 ? @" AND " + string.Join(@" AND ", additonalWhereStatements) : string.Empty;
		//	var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);

		//	// Put them together to create the SQL command.
		//	var sqlCmd = string.Format("{0}{1} WHERE {2}{3}", select.SelectClause, innerJoinStatement, primaryWhereClause, additionalWhereClause);
		//	var cmd = new SqlCommand(sqlCmd);
		//	var parameterWithValue =
		//		primaryQueryParameters
		//			.Select(kv => {
		//				var whereParameterValue = parameters[kv.Key];
		//				var sqlParameter = kv.Value;
		//				sqlParameter.Value = whereParameterValue ?? DBNull.Value;
		//				return sqlParameter;
		//			})
		//			.ToArray();
		//	cmd.Parameters.AddRange(parameterWithValue);
		//	foreach (var parameter in additionalSqlParameters)
		//	{
		//		cmd.Parameters.Add(parameter.Invoke());
		//	}
		//	this.LogSql("Select<T> with where expression", node, cmd);
		//	return ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
		//}       
		//public IEnumerable<T> Select<T>(
		//	string connectionString, 
		//	Node node, 
		//	IEnumerable<Expression<Func<T, bool>>> whereClauses, 
		//	IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
		//	IEnumerable<ValueTuple<NodeEdge, Expression>> childInnerJoinWhereClauses,
		//	IEnumerable<ValueTuple<NodeEdge, Expression[]>> childInnerJoinOrClausesGroup,
		//	int maxNumOfRow, 
		//	(NodeProperty Property, bool IsAscending)[] orderBy, 
		//	int timeoutDurationInSeconds = 30
		//	) where T : class, new()
		//{
		//	Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

		//	var tablePrefix = @"t";
		//	var tableAliasFirstIdx = 0;

		//	// set the current node, which is the T as the starting point.  All other inner joins should be derived from this point upwards towards parent reference.
		//	var innerJoinMap = new InnerJoinMap();
		//	var rootMapKey = innerJoinMap.Init(node, tableAliasFirstIdx);

		//	var (whereStatements, sqlParameterList) = this.CompileConditions<T>(node, whereClauses, orClausesGroup, tablePrefix, innerJoinMap);

		//	var select = this.sqlGenerator.CreateSelectComponent($"{tablePrefix}{tableAliasFirstIdx}", node, maxNumOfRow);
		//	var orderByClause = this.sqlGenerator.CompileOrderByClause(orderBy);
		//	var whereStatement = whereStatements.Count() > 0 ? @" WHERE " + string.Join(@" AND ", whereStatements) : string.Empty;
		//	var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);
		//	var sqlCmd = select.SelectClause + innerJoinStatement + whereStatement + orderByClause;
		//	var cmd = new SqlCommand(sqlCmd);
		//	foreach (var parameter in sqlParameterList)
		//	{
		//		cmd.Parameters.Add(parameter.Invoke());
		//	}
		//	this.LogSql("Select<T> with where expression", node, cmd);
		//	return ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
		//}

		#endregion
	}
}