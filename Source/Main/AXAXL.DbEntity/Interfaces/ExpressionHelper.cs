using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	internal class ExpressionHelper
	{
		internal static readonly MethodInfo enumerableCastMethodInfo = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast), BindingFlags.Public | BindingFlags.Static);

		/// <summary>
		/// Restore IEnumerable of Expression to IEnumerable of Expression&lt;Func&lt;T, bool&gt;&gt;.
		/// </summary>
		/// <param name="node">Type of this node will be the type of the delegate to be restored.</param>
		/// <param name="whereClauses">list of expression</param>
		/// <param name="restoredType">type of the original expression</param>
		/// <returns>Restored expression of the right type</returns>
		internal static object RestoreWhereClause(Node node, IEnumerable<Expression> whereClauses, out Type restoredType)
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
		internal static object RestoreOrClauses(Node node, IEnumerable<Expression[]> orClauses, out Type restoredType)
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
	}
}
