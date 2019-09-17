using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.Interfaces;
using ExpressionToString;

namespace AXAXL.DbEntity.MSSql
{
	public class WhereClauseVisitor<T> : ExpressionVisitor
	{
		private ILogger log = null;
		private Node startingNode = null;
		private IOrderedDictionary innerJoinMap = null;
		private IDictionary<Type, SqlDbType> csharp2SqlTypeMap = null;
		private IQueryExtensionForSqlOperators extensions = null;
		private StringBuilder buffer = null;
		private List<(string parameter, Type exprResultType, Expression expression)> captured = new List<(string, Type, Expression)>();
		private int sqlParameterRunningSeq = 0;
		private string tableAliasPrefix;
		private readonly IDictionary<ExpressionType, string> operators = new Dictionary<ExpressionType, string>
		{
			[ExpressionType.Equal] = @" = ",
			[ExpressionType.GreaterThan] = @" > ",
			[ExpressionType.GreaterThanOrEqual] = @" >= ",
			[ExpressionType.LessThan] = @" < ",
			[ExpressionType.LessThanOrEqual] = @" <= ",
			[ExpressionType.NotEqual] = @" <> "
		};
		internal WhereClauseVisitor(
			Node startingNode, 
			int sqlParameterRunningSeq, 
			string tableAliasPrefix, 
			ILogger log, 
			IOrderedDictionary innerJoinMap, 
			IDictionary<Type, SqlDbType> typeMap, 
			IQueryExtensionForSqlOperators extensions
			)
		{
			this.log = log;
			this.innerJoinMap = innerJoinMap;
			this.csharp2SqlTypeMap = typeMap;
			this.buffer = new StringBuilder(@" WHERE ");
			this.extensions = extensions;
			this.sqlParameterRunningSeq = sqlParameterRunningSeq;
			this.startingNode = startingNode;
			this.tableAliasPrefix = tableAliasPrefix;
		}
		internal (int SqlParameterRunningSeq, string WhereClause, string InnerJoinClause, Func<SqlParameter>[] SqlParameters) Compile(Expression<Func<T, bool>> whereClause)
		{
			this.Visit(whereClause);
			var where = this.buffer.ToString();
			var innerJoins = this.ComputeInnerJoins(this.innerJoinMap);
			var sqlParameters = this.captured.Select(p => this.CreateSqlParameterFromCaptured(p)).ToArray();
			return (this.sqlParameterRunningSeq, where, innerJoins, sqlParameters);
		}
		private Func<SqlParameter> CreateSqlParameterFromCaptured((string parameter, Type exprResultType, Expression expression) captured)
		{
			var sqlParameterVariable = Expression.Variable(typeof(SqlParameter), "sqlParam");
			var parameterName = Expression.Property(sqlParameterVariable, "ParameterName");
			var parameterType = Expression.Property(sqlParameterVariable, "SqlDbType");
			var parameterValue = Expression.Property(sqlParameterVariable, "Value");
			var returnLabel = Expression.Label(typeof(SqlParameter), "return");
			var dbType = this.csharp2SqlTypeMap[captured.exprResultType];
			var expressions = new Expression[5];
			expressions[0] = Expression.Assign(sqlParameterVariable, Expression.New(typeof(SqlParameter)));
			expressions[1] = Expression.Assign(parameterName, Expression.Constant(captured.parameter, typeof(string)));
			expressions[2] = Expression.Assign(parameterType, Expression.Constant(dbType, typeof(SqlDbType)));
			expressions[3] = Expression.Assign(parameterValue, Expression.Convert(captured.expression, typeof(object)));
			expressions[4] = Expression.Label(returnLabel, sqlParameterVariable);
			var block = Expression.Block(new[] { sqlParameterVariable }, expressions);
			var func = Expression.Lambda<Func<SqlParameter>>(block);

			this.LoqSqlParameterCreationExpression(func);

			return func.Compile();
		}
		[Conditional("DEBUG")]
		private void LoqSqlParameterCreationExpression(Expression argExpr)
		{
			this.log.LogDebug("Sql Parameter Creation ... ", argExpr.ToString("C#"));
		}
		protected override Expression VisitBinary(BinaryExpression node)
		{
			switch (node.NodeType)
			{
				case ExpressionType.AndAlso:
				case ExpressionType.OrElse:
					buffer.Append("(");
					this.Visit(node.Left);
					buffer.Append(")");
					buffer.Append(node.NodeType == ExpressionType.AndAlso ? " AND " : " OR ");
					buffer.Append("(");
					this.Visit(node.Right);
					buffer.Append(")");
					break;
				case ExpressionType.Equal:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.NotEqual:
					this.Visit(node.Left);
					buffer.Append(this.operators[node.NodeType]);
					this.Visit(node.Right);
					break;
					// case ExpressionType.MemberAccess:
					//     var member = node as MemberExpression;
					//     buffer.Append(member.Member.Name);
					//     break;
					// case ExpressionType.Constant:
					// case ExpressionType.Call:
					// case ExpressionType.Convert:
					//     buffer.Append($"@Parameter{++seq}");
					//     break;
			}
			return node;
		}
		protected override Expression VisitConstant(ConstantExpression constant)
		{
			var parameterName = $"@Parameter{++sqlParameterRunningSeq}";
			buffer.Append(parameterName);
			this.captured.Add((parameterName, constant.Type, constant));
			return constant;
		}
		protected override Expression VisitMember(MemberExpression member)
		{
			var names = new Stack<string>();
			var isAnEntityProperty = this.IsAnEntityProperty(member, names);
			//var parent = member.Member.DeclaringType;
			//if (parent.IsAssignableFrom(typeof(T)) == false)
			if (! isAnEntityProperty)
			{
				var parameterName = $"@Parameter{++sqlParameterRunningSeq}";
				buffer.Append(parameterName);
				this.captured.Add((parameterName, member.Type, member));
			}
			else
			{
				var propertyName = member.Member.Name;
				
				//var columnName = this.node.GetDbColumnNameFromPropertyName(propertyName);
				//Debug.Assert(string.IsNullOrEmpty(columnName) == false, $"Failed to locate DB Column for property '{propertyName}' on '{this.node.NodeType.Name}'");

				var columnName = this.GetDbColumnName(this.startingNode, @"-", propertyName, names, this.innerJoinMap);
				Debug.Assert(string.IsNullOrEmpty(columnName) == false, $"Failed to locate DB Column for property '{propertyName}'");
				buffer.Append(columnName);
			}
			return member;
		}
		protected override Expression VisitUnary(UnaryExpression unary)
		{
			var parameterName = $"@Parameter{++sqlParameterRunningSeq}";
			buffer.Append(parameterName);
			this.captured.Add((parameterName, unary.Type, unary));
			return unary;
		}
		protected override Expression VisitMethodCall(MethodCallExpression method)
		{
			if (this.extensions.IsSupported(method))
			{
				var translation = this.extensions.Translate(method);
				if (translation.LeftNode != null)
				{
					this.Visit(translation.LeftNode);
				}
				this.buffer.Append(translation.SqlOperator);
				if (translation.RightNode != null)
				{
					this.Visit(translation.RightNode);
				}
			}
			else
			{
				var arguments = method.Arguments;
				if (arguments.Count > 1 && arguments.Any(a => a.NodeType == ExpressionType.MemberAccess && (a as MemberExpression).Member.DeclaringType == typeof(T)))
				{
					throw new ArgumentException($"Does not support method with multiple arguments with on of them is a property");
				}
				// buffer.Append($"{method.Method.Name}(");
				// foreach (var eachArgument in method.Arguments)
				// {
				//	this.Visit(eachArgument);
				// }
				// buffer.Append($")");

				var parameterName = $"@Parameter{++sqlParameterRunningSeq}";
				buffer.Append(parameterName);
				this.captured.Add((parameterName, method.Type, method));
			}
			return method;
		}
		protected override Expression VisitNew(NewExpression newExpr)
		{
			var arguments = newExpr.Arguments;
			if (arguments.Any(a => a.NodeType == ExpressionType.MemberAccess && (a as MemberExpression).Member.DeclaringType == typeof(T)))
			{
				throw new ArgumentException($"Does not support creating new target object within where clause");
			}
			var parameterName = $"@Parameter{++sqlParameterRunningSeq}";
			buffer.Append(parameterName);
			this.captured.Add((parameterName, newExpr.Type, newExpr));

			return newExpr;
		}
		private bool IsAnEntityProperty(MemberExpression member, Stack<string> names)
		{
			if (member.Expression is MemberExpression parent)
			{
				names.Push(parent.Member.Name);
				return this.IsAnEntityProperty(parent, names);
			}
			else
			{
				return member.Expression is ParameterExpression;
			}
		}
		private string GetDbColumnName(Node current, string edgeName, string propertyName, Stack<string> path, IOrderedDictionary innerJoinMap)
		{
			if (path.Count() <= 0)
			{
				var aliasIdx = ((ValueTuple<int, int, NodeEdge>)innerJoinMap[edgeName]).Item1;
				var dbColumnName = current.GetDbColumnNameFromPropertyName(propertyName);
				return $"{this.tableAliasPrefix}{aliasIdx}.[{dbColumnName}]";
			}
			else
			{
				var entityRef = path.Pop();
				var edge = this.AddEdgeToInnerJoin(innerJoinMap, current, edgeName, entityRef);
				return this.GetDbColumnName(edge.ParentNode, entityRef, propertyName, path, innerJoinMap);
			}
		}
		private NodeEdge AddEdgeToInnerJoin(IOrderedDictionary innerJoins, Node currentNode, string currentEdgeName, string newJoin)
		{
			NodeEdge edge = null;

			if (! innerJoins.Contains(newJoin))
			{
				var parentRefOnChild = currentNode.GetPropertyFromNode(newJoin);
				Debug.Assert(parentRefOnChild != null);
				Debug.Assert(parentRefOnChild.IsEdge);

				int currentIdx = ((ValueTuple<int, int, NodeEdge>)innerJoins[currentEdgeName]).Item1;
				int lastIdx = ((ValueTuple<int, int, NodeEdge>)innerJoins[innerJoins.Count - 1]).Item1;
				edge = currentNode.GetEdgeToParent(parentRefOnChild);
				(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) innerJoin = (ParentTableAliasIdx: ++lastIdx, ChildTableAliasIdx: currentIdx, Edge: edge);

				innerJoins.Add(newJoin, innerJoin);
			}
			else
			{
				edge = ((ValueTuple<int, int, NodeEdge>)innerJoins[newJoin]).Item3;
			}
			return edge;
		}

		private string ComputeInnerJoins(IOrderedDictionary innerJoinsMap)
		{
			(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) eachEdge;
			StringBuilder buffer = new StringBuilder();
			var enumerator = innerJoinsMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				eachEdge = (ValueTuple<int, int, NodeEdge>)enumerator.Value;

				if (eachEdge.Edge == null) continue;

				var parentAlias = $"{this.tableAliasPrefix}{eachEdge.ParentTableAliasIdx}";
				var childAlias = $"{this.tableAliasPrefix}{eachEdge.ChildTableAliasIdx}";
				var nodeEdge = eachEdge.Edge;
				var parentTable = FormatTableName(nodeEdge.ParentNode, parentAlias);
				var childTable = FormatTableName(nodeEdge.ChildNode, childAlias);
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
		/// <summary>
		/// Format table name from Node data.
		/// </summary>
		/// <remarks>
		/// Straightly copied from <see cref="MSSqlGenerator"/>. Need to find a way to share this code.
		/// </remarks>
		/// <param name="node">target node</param>
		/// <param name="tableAlias">table alias to be used in SQL</param>
		/// <returns></returns>
		private static string FormatTableName(Node node, string tableAlias = null)
		{
			Debug.Assert(node != null, $"Input node is null!");
			var tableName = string.IsNullOrEmpty(node.DbSchemaName) ? $"[{node.DbTableName}]" : $"[{node.DbSchemaName}].[{node.DbTableName}]";
			return tableAlias == null ? $"{tableName}" : $"{tableName} AS {tableAlias}";
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
	}
}
