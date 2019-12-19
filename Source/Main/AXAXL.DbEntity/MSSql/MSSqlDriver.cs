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
	public partial class MSSqlDriver : IDatabaseDriver
	{
		private readonly ILogger log = null;
		private readonly IMSSqlGenerator sqlGenerator = null;
		private static readonly MethodInfo enumerableCastMethodInfo = typeof(Enumerable).GetMethod("Cast", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo compileWhereConditionsMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(CompileWhereConditions), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo compileOrGroupsMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(CompileOrGroups), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo selectImplementationMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(SelectImplementation), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo selectByMultipleValuesImplementationMethodInfo = typeof(MSSqlDriver).GetMethod(nameof(SelectByMultipleValuesImplementation), BindingFlags.NonPublic | BindingFlags.Instance);
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

			return this.SelectImplementation<T>(
							connectionString, 
							node, 
							parameters, 
							new Expression<Func<T, bool>>[0], 
							new List<Expression<Func<T, bool>>[]>(), 
							new List<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)>(),
							new List<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)>(),
							-1,
							null,
							timeoutDurationInSeconds
							);

			/* replaced original code to call SelectImplementation also. 2019-12-17			
			 *			var aliasT0 = @"t0";
						var select = this.sqlGenerator.CreateSelectComponent(@"t0", node);
						var whereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
						var queryParameters = this.sqlGenerator.CreateSqlParameters(node, whereColumns);
						var whereClause = this.sqlGenerator.CreateWhereClause(node, whereColumns, tableAlias: aliasT0);
						var sql = this.FormatSelectStatement(node, select.SelectedColumns, null, whereClause, null, null, null, null, null, null, aliasT0, -1, null);
						var cmd = new SqlCommand(sql);

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
			*/
		}

		public IEnumerable<T> Select<T>(
			string connectionString,
			Node node,
			IDictionary<string, object> parameters,
			IEnumerable<Expression> additionalWhereClauses, 
			IEnumerable<Expression[]> additionalOrClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
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
														typeof(IList<(IList<NodeEdge>, Node, IEnumerable<Expression>)>),
														typeof(IList<(IList<NodeEdge>, Node, IEnumerable<Expression[]>)>),
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
			IDictionary<string, object[]> parameters,
			IEnumerable<Expression> additionalWhereClauses,
			IEnumerable<Expression[]> additionalOrClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
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
														typeof(IDictionary<string, object[]>),
														typeOfAdditionalWhereClauses,
														typeOfAdditionalOrClauses,
														typeof(IList<(IList<NodeEdge>, Node, IEnumerable<Expression>)>),
														typeof(IList<(IList<NodeEdge>, Node, IEnumerable<Expression[]>)>),
														typeof(int),
														typeof(ValueTuple<NodeProperty, bool>[]),
														typeof(int),
														enumerableOfTType
														);
			var delegateHandle = selectByMultipleValuesImplementationMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(selectDelegateType, this);
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
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy,
			int timeoutDurationInSeconds = 30
			) where T : class, new()
		{
			return this.SelectImplementation<T>(connectionString, node, MSSqlDriver.emptyParameters, whereClauses, orClausesGroup, childInnerJoinWhereClauses, childInnerJoinOrClausesGroup, maxNumOfRow, orderBy, timeoutDurationInSeconds);
		}
		// TODO: Complete changes on retrieving all grand-children of children in one shot.
		[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL generated are using SQL parameters for user input.")]
		private IEnumerable<T> SelectByMultipleValuesImplementation<T>(
			string connectionString,
			Node node,
			IDictionary<string, object[]> parameters,
			IEnumerable<Expression<Func<T, bool>>> whereClauses,
			IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy,
			int timeoutDurationInSeconds = 30
			) where T : class, new()
		{
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false, "Connection string has not been setup yet");

			List<T> resultSet = new List<T>();
			var tablePrefix = @"t";
			var tableAliasFirstIdx = 0;
			int sqlParameterRunningSeq = 0;
			var topLevelTableAlias = $"{tablePrefix}{tableAliasFirstIdx}";

			var select = this.sqlGenerator.CreateSelectComponent(topLevelTableAlias, node);
			var primaryWhereTuples = this.sqlGenerator.CreateWhereClauseAndSqlParametersFromKeyValues(node, parameters, tableAlias: topLevelTableAlias);
			//var primaryWhereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			//var primaryQueryParameters = this.sqlGenerator.CreateSqlParameters(node, primaryWhereColumns);
			//var primaryWhereStatement = this.sqlGenerator.CreateWhereClause(node, primaryWhereColumns, tableAlias: topLevelTableAlias);
			//var orderByClause = this.sqlGenerator.CompileOrderByClause(orderBy, topLevelTableAlias);

			// set the current node, which is the T as the starting point.  All other inner joins should be derived from this point upwards towards parent reference.
			var innerJoinMap = new InnerJoinMap();
			var rootMapKey = innerJoinMap.Init(node, tableAliasFirstIdx);
			var additionalWhere = this.CompileWhereConditions<T>(node, whereClauses, tablePrefix, rootMapKey, sqlParameterRunningSeq, innerJoinMap);
			var additionalOr = this.CompileOrGroups<T>(node, orClausesGroup, tablePrefix, rootMapKey, additionalWhere.Item3, innerJoinMap);
			//var (innerJoinWhereStatements, innerJoinSqlParameters) = this.CompileInnerJoinWhere(node, rootMapKey, childInnerJoinWhereClauses, tablePrefix, innerJoinMap);

			// Find the Inner Joins with the parent at the edge at the head of path the same as current node.
			// Extract those inner join. Add those path into inner join map to create inner join statement later.
			// Also Compile the where clause attached to the child node of the edge at the end of the path into where statement and parameters.
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhereForThisNode;
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOrForThisNode;
			IEnumerable<Expression> additionalWhereForThisNode;
			IEnumerable<Expression[]> additionalOrForThisNode;
			var innerJoinWhereFound = this.MatchEdgeToInnerJoinPaths<Expression>(node, childInnerJoinWhereClauses, out innerJoinWhereForThisNode, out additionalWhereForThisNode);
			var innerJoinOrFound = this.MatchEdgeToInnerJoinPaths<Expression[]>(node, childInnerJoinOrClausesGroup, out innerJoinOrForThisNode, out additionalOrForThisNode);
			var additionalInnerJoinWhere = this.CompileChildInnerJoinWhere(innerJoinWhereForThisNode, tablePrefix, additionalOr.Item3, innerJoinMap);
			var additionalInnerJoinOr = this.CompileChildInnerJoinOrGroup(innerJoinOrForThisNode, tablePrefix, additionalInnerJoinWhere.Item3, innerJoinMap);
			var additionalWhereStatementForThisNode = this.CompileWhereConditionsFromExpressions(node, additionalWhereForThisNode, tablePrefix, rootMapKey, additionalInnerJoinWhere.Item3, innerJoinMap);
			var additionalOrStatementForThisNode = this.CompileOrGroupsFromExpression(node, additionalOrForThisNode, tablePrefix, rootMapKey, additionalWhereStatementForThisNode.Item3, innerJoinMap);

			// Completed inner join work.  Remove the top edge from the path worked on, so that when stepping down the entity graph, the children along the path will create the same 
			// inner join conditions.
			this.RemovedVisited<Expression>(childInnerJoinWhereClauses, innerJoinWhereFound);
			this.RemovedVisited<Expression[]>(childInnerJoinOrClausesGroup, innerJoinOrFound);

			var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);
			//var whereStatements = this.CombineWhereStatements(true, primaryWhereStatement, additionalWhere.Item1, additionalOr.Item1, additionalInnerJoinWhere.Item1, additionalInnerJoinOr.Item1);
			//var sqlCmd = string.Format("{0}{1}{2}{3}", select.SelectClause, innerJoinStatement, whereStatements, orderByClause);

			foreach(var eachPrimaryWhere in primaryWhereTuples)
			{
				var sqlCmd = this.FormatSelectStatement(
					node,
					select.SelectedColumns,
					innerJoinStatement,
					eachPrimaryWhere.primaryWhereClause,
					additionalWhere.Item1,
					additionalOr.Item1,
					additionalWhereStatementForThisNode.Item1,
					additionalOrStatementForThisNode.Item1,
					additionalInnerJoinWhere.Item1,
					additionalInnerJoinOr.Item1,
					topLevelTableAlias,
					maxNumOfRow,
					orderBy
					);
				IEnumerable<T> resultOfOneBatch = null;
				using (SqlCommand cmd = new SqlCommand(sqlCmd))
				{
					cmd.Parameters.AddRange(eachPrimaryWhere.primaryWhereParameters);
					this
						.InvokeAndAddSqlParameters(cmd, additionalWhere.Item2)
						.InvokeAndAddSqlParameters(cmd, additionalOr.Item2)
						.InvokeAndAddSqlParameters(cmd, additionalInnerJoinWhere.Item2)
						.InvokeAndAddSqlParameters(cmd, additionalInnerJoinOr.Item2)
						.InvokeAndAddSqlParameters(cmd, additionalWhereStatementForThisNode.Item2)
						.InvokeAndAddSqlParameters(cmd, additionalOrStatementForThisNode.Item2)
						;
					this.LogSql("SelectImplementation", node, cmd);
					resultOfOneBatch = ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
				}
				resultSet.AddRange(resultOfOneBatch);
			}
			return resultSet;
		}

		[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL generated are using SQL parameters for user input.")]
		private IEnumerable<T> SelectImplementation<T>(
			string connectionString,
			Node node,
			IDictionary<string, object> parameters,
			IEnumerable<Expression<Func<T, bool>>> whereClauses,
			IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
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

			var select = this.sqlGenerator.CreateSelectComponent(topLevelTableAlias, node);

			var primaryWhereColumns = this.sqlGenerator.ExtractColumnByPropertyName(node, parameters.Keys.ToArray());
			var primaryQueryParameters = this.sqlGenerator.CreateSqlParameters(node, primaryWhereColumns);
			var primaryWhereStatement = this.sqlGenerator.CreateWhereClause(node, primaryWhereColumns, tableAlias: topLevelTableAlias);
			//var orderByClause = this.sqlGenerator.CompileOrderByClause(orderBy, topLevelTableAlias);

			// set the current node, which is the T as the starting point.  All other inner joins should be derived from this point upwards towards parent reference.
			var innerJoinMap = new InnerJoinMap();
			var rootMapKey = innerJoinMap.Init(node, tableAliasFirstIdx);
			var additionalWhere = this.CompileWhereConditions<T>(node, whereClauses, tablePrefix, rootMapKey, sqlParameterRunningSeq, innerJoinMap);
			var additionalOr = this.CompileOrGroups<T>(node, orClausesGroup, tablePrefix, rootMapKey, additionalWhere.Item3, innerJoinMap);
			//var (innerJoinWhereStatements, innerJoinSqlParameters) = this.CompileInnerJoinWhere(node, rootMapKey, childInnerJoinWhereClauses, tablePrefix, innerJoinMap);

			// Find the Inner Joins with the parent at the edge at the head of path the same as current node.
			// Extract those inner join. Add those path into inner join map to create inner join statement later.
			// Also Compile the where clause attached to the child node of the edge at the end of the path into where statement and parameters.
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhereForThisNode;
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOrForThisNode;
			IEnumerable<Expression> additionalWhereForThisNode;
			IEnumerable<Expression[]> additionalOrForThisNode;
			var innerJoinWhereFound = this.MatchEdgeToInnerJoinPaths<Expression>(node, childInnerJoinWhereClauses, out innerJoinWhereForThisNode, out additionalWhereForThisNode);
			var innerJoinOrFound = this.MatchEdgeToInnerJoinPaths<Expression[]>(node, childInnerJoinOrClausesGroup, out innerJoinOrForThisNode, out additionalOrForThisNode);
			var additionalInnerJoinWhere = this.CompileChildInnerJoinWhere(innerJoinWhereForThisNode, tablePrefix, additionalOr.Item3, innerJoinMap);
			var additionalInnerJoinOr = this.CompileChildInnerJoinOrGroup(innerJoinOrForThisNode, tablePrefix, additionalInnerJoinWhere.Item3, innerJoinMap);
			var additionalWhereStatementForThisNode = this.CompileWhereConditionsFromExpressions(node, additionalWhereForThisNode, tablePrefix, rootMapKey, additionalInnerJoinWhere.Item3, innerJoinMap);
			var additionalOrStatementForThisNode = this.CompileOrGroupsFromExpression(node, additionalOrForThisNode, tablePrefix, rootMapKey, additionalWhereStatementForThisNode.Item3, innerJoinMap);

			// Completed inner join work.  Remove the top edge from the path worked on, so that when stepping down the entity graph, the children along the path will create the same 
			// inner join conditions.
			this.RemovedVisited<Expression>(childInnerJoinWhereClauses, innerJoinWhereFound);
			this.RemovedVisited<Expression[]>(childInnerJoinOrClausesGroup, innerJoinOrFound);

			var innerJoinStatement = this.ComputeInnerJoins(innerJoinMap, tablePrefix);
			//var whereStatements = this.CombineWhereStatements(true, primaryWhereStatement, additionalWhere.Item1, additionalOr.Item1, additionalInnerJoinWhere.Item1, additionalInnerJoinOr.Item1);
			//var sqlCmd = string.Format("{0}{1}{2}{3}", select.SelectClause, innerJoinStatement, whereStatements, orderByClause);
			var sqlCmd = this.FormatSelectStatement(
				node, 
				select.SelectedColumns, 
				innerJoinStatement, 
				primaryWhereStatement, 
				additionalWhere.Item1, 
				additionalOr.Item1,
				additionalWhereStatementForThisNode.Item1,
				additionalOrStatementForThisNode.Item1,
				additionalInnerJoinWhere.Item1, 
				additionalInnerJoinOr.Item1,
				topLevelTableAlias,
				maxNumOfRow,
				orderBy
				);
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
				this
					.InvokeAndAddSqlParameters(cmd, additionalWhere.Item2)
					.InvokeAndAddSqlParameters(cmd, additionalOr.Item2)
					.InvokeAndAddSqlParameters(cmd, additionalInnerJoinWhere.Item2)
					.InvokeAndAddSqlParameters(cmd, additionalInnerJoinOr.Item2)
					.InvokeAndAddSqlParameters(cmd, additionalWhereStatementForThisNode.Item2)
					.InvokeAndAddSqlParameters(cmd, additionalOrStatementForThisNode.Item2)
					;
				this.LogSql("SelectImplementation", node, cmd);
				resultSet = ExecuteQuery<T>(connectionString, select.DataReaderToEntityFunc, cmd, timeoutDurationInSeconds);
			}
			return resultSet;
		}

		private string FormatSelectStatement(
			Node node,
			string selectedColumns, 
			string innerJoinStatement, 
			string primaryWhereStatement, 
			string additionalWhereStatements, 
			string additionalOrGroupStatements, 
			string additionalWhereFromEndOfPathInnerJoin,
			string additionalOrGroupFromEndOfPathInnerJoin,
			string additionalInnerJoinWhereStatements, 
			string additionalInnerJoinOrGroups,
			string tableAlias,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy
			)
		{
			bool requireDistinct = false;
			string selectStatement;
			var tableName = this.sqlGenerator.FormatTableName(node, tableAlias);
			var combinedWhere = this.CombineWhereStatements(
										true, 
										primaryWhereStatement, 
										additionalWhereStatements, 
										additionalOrGroupStatements,
										additionalWhereFromEndOfPathInnerJoin,
										additionalOrGroupFromEndOfPathInnerJoin,
										additionalInnerJoinWhereStatements, 
										additionalInnerJoinOrGroups);

			if (! string.IsNullOrEmpty(additionalInnerJoinWhereStatements) || ! string.IsNullOrEmpty(additionalInnerJoinOrGroups))
			{
				requireDistinct = true;
			}
			if (requireDistinct)
			{
				selectStatement = string.Format(
										@"SELECT DISTINCT {0} FROM {1}{2}{3}",
										selectedColumns,
										tableName,
										innerJoinStatement,
										combinedWhere
									);
				if (maxNumOfRow > 0)
				{
					var orderByStatement = this.sqlGenerator.CompileOrderByClause(orderBy, @"distinct_result");
					selectStatement = string.Format(@"SELECT TOP {0} distinct_result.* FROM ({1}) AS distinct_result{2}", maxNumOfRow, selectStatement, orderByStatement);
				}
				else
				{
					selectStatement += this.sqlGenerator.CompileOrderByClause(orderBy, tableAlias);
				}
			}
			else
			{
				selectStatement = string.Format(
										@"SELECT {0}{1} FROM {2}{3}{4}{5}",
										maxNumOfRow > 0 ? $"TOP {maxNumOfRow} " : String.Empty,
										selectedColumns,
										tableName,
										innerJoinStatement,
										combinedWhere,
										this.sqlGenerator.CompileOrderByClause(orderBy, tableAlias)
									 );
			}
			return selectStatement;
		}
		private MSSqlDriver InvokeAndAddSqlParameters(SqlCommand sqlCommand, IEnumerable<Func<SqlParameter>> parameterDelegates)
		{
			foreach(var eachDelegate in parameterDelegates)
			{
				sqlCommand.Parameters.Add(eachDelegate.Invoke());
			}
			return this;
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
		/// Construct delegate of <seealso cref="CompileWhereConditions{TEntity}"/>
		/// with the right type for its generic type parameter.
		/// </summary>
		/// <param name="node">Node type will be used as the type parameter of the generic method <see cref="CompileConditions{TEntity}(Node, IEnumerable{Expression{Func{TEntity, bool}}}, IEnumerable{Expression{Func{TEntity, bool}}[]}, string, OrderedDictionary)"/></param>
		/// <param name="whereClausesType">type of the where clauses</param>
		/// <returns>delegate to call the CompileConditions method</returns>
		private Delegate MakeCompileWhereWithRightType(Node node, Type whereClausesType)
		{
			var resultingValueTupleType = typeof(ValueTuple<string, List<Func<SqlParameter>>, int>);
			var compileConditionsDelegateType = typeof(Func<,,,,,,>)
													.MakeGenericType(
														typeof(Node), 
														whereClausesType, 
														typeof(string),
														typeof(string),
														typeof(int),
														typeof(IInnerJoinMap),
														resultingValueTupleType
														);
			var delegateHandle = compileWhereConditionsMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(compileConditionsDelegateType, this);
			return delegateHandle;
		}

		private Delegate MakeCompileOrWithRightType(Node node, Type orClausesType)
		{
			var resultingValueTupleType = typeof(ValueTuple<string, List<Func<SqlParameter>>, int>);
			var compileOrGroupDelegateType = typeof(Func<,,,,,,>)
													.MakeGenericType(
														typeof(Node),
														orClausesType,
														typeof(string),
														typeof(string),
														typeof(int),
														typeof(IInnerJoinMap),
														resultingValueTupleType
														);
			var delegateHandle = compileOrGroupsMethodInfo.MakeGenericMethod(node.NodeType).CreateDelegate(compileOrGroupDelegateType, this);
			return delegateHandle;
		}
		private (string, List<Func<SqlParameter>>, int) CompileWhereConditions<TEntity>
		(
			Node node,
			IEnumerable<Expression<Func<TEntity, bool>>> whereClauses,
			string tablePrefix,
			string rootMapKey,
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
										rootMapKey,
										innerJoinMap,
										where
										);
				sqlParameterRunningSeq = compilationResult.parameterSequence;
				whereStatements.Add(compilationResult.whereClause);
				sqlParameterList.AddRange(compilationResult.sqlParameters);
			}
			return (this.CombineWhereStatements(false, whereStatements), sqlParameterList, sqlParameterRunningSeq);
		}

		private (string, List<Func<SqlParameter>>, int) CompileWhereConditionsFromExpressions (
			Node node,
			IEnumerable<Expression> whereClauses, 
			string tablePrefix,
			string rootMapKey,
			int sqlParameterRunningSeq, 
			IInnerJoinMap innerJoinMap
			)
		{
			(string, List<Func<SqlParameter>>, int) compilationResult;
			Type restoredWhereClausesType;
			var restoredWhereClauses = this.RestoreWhereClause(node, whereClauses, out restoredWhereClausesType);
			var compileDelegate = this.MakeCompileWhereWithRightType(node, restoredWhereClausesType);
			compilationResult = (ValueTuple<string, List<Func<SqlParameter>>, int>)compileDelegate.DynamicInvoke(node, restoredWhereClauses, tablePrefix, rootMapKey, sqlParameterRunningSeq, innerJoinMap);
			return compilationResult;
		}

		private	(string, List<Func<SqlParameter>>, int) CompileOrGroups<TEntity>
		(
			Node node, 
			IEnumerable<Expression<Func<TEntity, bool>>[]> orClausesGroup, 
			string tablePrefix,
			string rootMapKey,
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
											rootMapKey,
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
			return (this.CombineWhereStatements(false, whereStatements), sqlParameterList, sqlParameterRunningSeq);
		}
		private (string, List<Func<SqlParameter>>, int) CompileOrGroupsFromExpression (
			Node node,
			IEnumerable<Expression[]> orGroupExpression,
			string tablePrefix,
			string rootMapKey,
			int runningSqlParameterSeq, 
			IInnerJoinMap innerJoinMap
			)
		{
			(string, List<Func<SqlParameter>>, int) compilationResult;
			Type restoredOrGroupType;
			var restoredOrGroupClause = this.RestoreOrClauses(node, orGroupExpression, out restoredOrGroupType);
			var compileDelegate = this.MakeCompileOrWithRightType(node, restoredOrGroupType);
			compilationResult = (ValueTuple<string, List<Func<SqlParameter>>, int>)compileDelegate.DynamicInvoke(node, restoredOrGroupClause, tablePrefix, rootMapKey, runningSqlParameterSeq, innerJoinMap);
			return compilationResult;
		}

		private string CombineWhereStatements(bool addWhere, params string[] whereStatements)
		{
			return this.CombineWhereStatements(addWhere, whereStatements.AsEnumerable());
		}

		private string CombineWhereStatements(bool addWhere, IEnumerable<string> whereStatements)
		{
			if (whereStatements == null || whereStatements.Count() <= 0 || whereStatements.Any(s => string.IsNullOrEmpty(s) == false) == false) return string.Empty;
			var whereKeyWord = addWhere ? @" WHERE " : String.Empty;
			var combined = string.Join(@" AND ", whereStatements.Where(s => string.IsNullOrEmpty(s) == false));
			return $"{whereKeyWord}{combined}";
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
					.Append(eachEdge.isTowardParent ? parentTable : childTable)
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

		/// <summary>
		/// This method will match the current node with the head of the Path which a list of edge from parent to child.
		/// If match, that means the path should form the inner joins list and the where clauses should be included to narrow
		/// the selection of the current node as in an inner join SQL query.
		/// If the Path in value tuple is empty, that means the current node is the child node in the path and the where clauses are the
		/// additional where condition for this child node in addition to the foreign keys.
		/// </summary>
		/// <typeparam name="TE">Type of expression, i.e. either Expression or Expression[]</typeparam>
		/// <param name="node">Current node of the Select</param>
		/// <param name="inputList">List of inner joins that we should examine.</param>
		/// <param name="matchedList">value tuple list with the Parent node of the first edge of Path matching current node.</param>
		/// <param name="additionalWhereForChild">where clauses in the inputlist where Path is empty, i.e. the code has reached the child node of the where clauses in value tuple.</param>
		/// <returns></returns>
		private IEnumerable<int> MatchEdgeToInnerJoinPaths<TE>(
			Node node,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<TE> Expressions)> inputList,
			out IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<TE> Expressions)> matchedList,
			out IEnumerable<TE> additionalWhereForChild
		)
		{
			var matchedIdx = new List<int>();
			var matched = new List<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<TE> Expressions)>();
			var childEdges = new HashSet<NodeEdge>(node.AllChildEdges());
			var additionalWhere = new List<TE>();

			for(int i = 0; i < inputList.Count; i++)
			{
				var eachTuple = inputList[i];
				if (eachTuple.Path.Count > 0)
				{
					if (childEdges.Contains(eachTuple.Path[0]))
					{
						matchedIdx.Add(i);
						matched.Add(eachTuple);
					}
				}
				else
				{
					if (node.Name == eachTuple.TargetChild.Name)
					{
						additionalWhere.AddRange(eachTuple.Expressions);
					}
				}
			}
			matchedList = matched;
			additionalWhereForChild = additionalWhere;
			return matchedIdx;
		}
		private IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<TE> expressions)> RemovedVisited<TE>(
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<TE> expressions)> input,
			IEnumerable<int> visitedIdx
			)
		{
			foreach(var eachIdx in visitedIdx)
			{
				var eachTuple = input[eachIdx];
				if (eachTuple.Path != null && eachTuple.Path.Count > 0)
				{
					eachTuple.Path.RemoveAt(0);
				}
			}
			return input;
		}

		private (string, List<Func<SqlParameter>>, int) CompileChildInnerJoinWhere(IEnumerable<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> innerJoinWheres, string tablePrefix, int runningSqlParameterSeq, IInnerJoinMap innerJoinMap)
		{
			List<string> whereStatements = new List<string>();
			List<Func<SqlParameter>> parameterList = new List<Func<SqlParameter>>();
			foreach(var innerJoinWhere in innerJoinWheres)
			{
				var compilationResult = this.CompileChildInnerJoinWhere(innerJoinWhere, tablePrefix, runningSqlParameterSeq, innerJoinMap);
				whereStatements.Add(compilationResult.Item1);
				parameterList.AddRange(compilationResult.Item2);
				runningSqlParameterSeq = compilationResult.Item3;
			}
			var combinedWhereStatement = this.CombineWhereStatements(false, whereStatements);
			return (combinedWhereStatement, parameterList, runningSqlParameterSeq);
		}

		private (string, List<Func<SqlParameter>>, int) CompileChildInnerJoinWhere((IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions) innerJoinWhere, string tablePrefix, int runningSqlParameterSeq, IInnerJoinMap innerJoinMap)
		{
			(string, List<Func<SqlParameter>>, int) compilationResult = ValueTuple.Create(string.Empty, new List<Func<SqlParameter>>(), runningSqlParameterSeq);
			(var newKey, var childNode) = this.InsertPathIntoInnerJoinMap(innerJoinWhere.Path, innerJoinMap);
			if (newKey != null)
			{
				compilationResult = CompileWhereConditionsFromExpressions(childNode, innerJoinWhere.Expressions, tablePrefix, newKey, runningSqlParameterSeq, innerJoinMap);
			}
			return compilationResult;
		}

		private (string, List<Func<SqlParameter>>, int) CompileChildInnerJoinOrGroup(IEnumerable<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> innerJoinOrGroups, string tablePrefix, int runningSqlParameterSeq, IInnerJoinMap innerJoinMap)
		{
			List<string> orStatements = new List<string>();
			List<Func<SqlParameter>> parameterList = new List<Func<SqlParameter>>();
			foreach (var innerJoinOrGroup in innerJoinOrGroups)
			{
				var compilationResult = this.CompileChildInnerJoinOrGroup(innerJoinOrGroup, tablePrefix, runningSqlParameterSeq, innerJoinMap);
				orStatements.Add(compilationResult.Item1);
				parameterList.AddRange(compilationResult.Item2);
				runningSqlParameterSeq = compilationResult.Item3;
			}
			var combinedStatements = this.CombineWhereStatements(false, orStatements);
			return (combinedStatements, parameterList, runningSqlParameterSeq);
		}

		private (string, List<Func<SqlParameter>>, int) CompileChildInnerJoinOrGroup((IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions) innerJoinOrGroup, string tablePrefix, int runningSqlParameterSeq, IInnerJoinMap innerJoinMap)
		{
			(string, List<Func<SqlParameter>>, int) compilationResult = ValueTuple.Create(string.Empty, new List<Func<SqlParameter>>(), runningSqlParameterSeq);
			(var newKey, var childNode) = this.InsertPathIntoInnerJoinMap(innerJoinOrGroup.Path, innerJoinMap);
			var orGroupExpression = innerJoinOrGroup.Expressions;
			if (newKey != null)
			{
				compilationResult = CompileOrGroupsFromExpression(childNode, orGroupExpression, tablePrefix, newKey, runningSqlParameterSeq, innerJoinMap);
			}
			return compilationResult;
		}

		private (string, Node) InsertPathIntoInnerJoinMap(IList<NodeEdge> path, IInnerJoinMap innerJoinMap)
		{
			var parentKey = innerJoinMap.RootMapKey;
			string newKey = null;
			Node childNode = null;
			foreach (var eachEdge in path)
			{
				newKey = innerJoinMap.Add(parentKey, eachEdge.ParentNode, eachEdge.ChildReferenceOnParentNode.PropertyName);
				parentKey = newKey;
				childNode = eachEdge.ChildNode;
			}
			return (newKey, childNode);
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
		private void LogSql(string message, Node node, SqlCommand cmd)
		{
			Debug.Assert(cmd != null);

			if (this.log.IsEnabled(LogLevel.Debug))
			{
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