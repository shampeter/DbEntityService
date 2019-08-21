using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IQuery<T> where T: class, new()
	{
		IList<T> ToList();
		T[] ToArray();
		IQuery<T> Where(Expression<Func<T, bool>> whereClause);
		IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions);
		// According to https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout?view=netcore-2.2, default SqlCommand timeout is 30 seconds.
		IQuery<T> SetTimeout(int timeoutDurationInSeconds = 30);
		IQuery<T> Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new();
	}
}
