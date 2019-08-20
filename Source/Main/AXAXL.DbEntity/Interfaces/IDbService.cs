using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbServiceOption
	{
		IDbServiceOption AddOrUpdateConnection(string connectionName, string connectionString);
		IDbServiceOption SetAsDefaultConnection(string connectionName);
		string GetDefaultConnectionString();
		string GetConnectionString(string connectionName);
	}
	public interface IDbService
	{
		IDbService Config(Action<IDbServiceOption> config);
		IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null);
		IQuery<T> Query<T>() where T : class, new();
		T Persist<T>(T entity) where T : class, ITrackable, new();
	}
	public interface IQuery<T> where T: class, new()
	{
		IList<T> ToList();
		T[] ToArray();
		IQuery<T> Where(Expression<Func<T, bool>> whereClause);
		IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions);
		IQuery<T> Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new();
	}
}
