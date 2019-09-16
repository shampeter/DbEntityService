using System;
using System.Diagnostics;
using System.Collections.Generic;
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
		private Node node = null;
		private IDictionary<Type, SqlDbType> csharp2SqlTypeMap = null;
		private IQueryExtensionForSqlOperators extensions = null;
		private StringBuilder buffer = null;
		private List<(string parameter, Type exprResultType, Expression expression)> captured = new List<(string, Type, Expression)>();
		private readonly IDictionary<ExpressionType, string> operators = new Dictionary<ExpressionType, string>
		{
			[ExpressionType.Equal] = @" = ",
			[ExpressionType.GreaterThan] = @" > ",
			[ExpressionType.GreaterThanOrEqual] = @" >= ",
			[ExpressionType.LessThan] = @" < ",
			[ExpressionType.LessThanOrEqual] = @" <= ",
			[ExpressionType.NotEqual] = @" <> "
		};
		private int seq = 0;
		internal WhereClauseVisitor(int runningSeq, ILogger log, Node node, IDictionary<Type, SqlDbType> typeMap, IQueryExtensionForSqlOperators extensions)
		{
			this.log = log;
			this.node = node;
			this.csharp2SqlTypeMap = typeMap;
			this.buffer = new StringBuilder(@" WHERE ");
			this.extensions = extensions;
			this.seq = runningSeq;
		}
		internal (int runningSeq, string WhereClause, Func<SqlParameter>[] SqlParameters) Compile(Expression<Func<T, bool>> whereClause)
		{
			this.Visit(whereClause);
			var where = this.buffer.ToString();
			var sqlParameters = this.captured.Select(p => this.CreateSqlParameterFromCaptured(p)).ToArray();
			return (this.seq, where, sqlParameters);
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
			var parameterName = $"@Parameter{++seq}";
			buffer.Append(parameterName);
			this.captured.Add((parameterName, constant.Type, constant));
			return constant;
		}
		protected override Expression VisitMember(MemberExpression member)
		{
			var names = new List<string>();
			var isFromParameter = this.IsComingFromParameter(member, names);
			//var parent = member.Member.DeclaringType;
			//if (parent.IsAssignableFrom(typeof(T)) == false)
			if (! isFromParameter)
			{
				var parameterName = $"@Parameter{++seq}";
				buffer.Append(parameterName);
				this.captured.Add((parameterName, member.Type, member));
			}
			else
			{
				var propertyName = member.Member.Name;
				var columnName = this.node.GetDbColumnNameFromPropertyName(propertyName);
				Debug.Assert(string.IsNullOrEmpty(columnName) == false, $"Failed to locate DB Column for property '{propertyName}' on '{this.node.NodeType.Name}'");
				buffer.Append(columnName);
			}
			return member;
		}
		protected override Expression VisitUnary(UnaryExpression unary)
		{
			var parameterName = $"@Parameter{++seq}";
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

				var parameterName = $"@Parameter{++seq}";
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
			var parameterName = $"@Parameter{++seq}";
			buffer.Append(parameterName);
			this.captured.Add((parameterName, newExpr.Type, newExpr));

			return newExpr;
		}
		private bool IsComingFromParameter(MemberExpression member, IList<string> names)
		{
			if (member.Expression is MemberExpression parent)
			{
				names.Add(parent.Member.Name);
				return this.IsComingFromParameter(parent, names);
			}
			else
			{
				return member.Expression is ParameterExpression;
			}
		}
	}
}
