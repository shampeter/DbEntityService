using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public static class Extensions
	{
		public static T FirstOrDefault<T>(this IQuery<T> query, Expression<Func<T, bool>> whereClause) where T: class, new()
		{
			return query.Where(whereClause).ToArray().FirstOrDefault();
		}
	}
}
