using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IQueryExtensionForSqlOperators
	{
		bool IsSupported(MethodCallExpression method);
		(Expression LeftNode, string SqlOperator, Expression RightNode) Translate(MethodCallExpression method);
	}
}
