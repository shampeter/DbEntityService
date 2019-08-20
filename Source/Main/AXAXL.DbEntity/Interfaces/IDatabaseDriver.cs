using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDatabaseDriver
	{
		T Insert<T>(string connectionString, T entity, Node node) where T : class, ITrackable, new();
		T Delete<T>(string connectionString, T entity, Node node) where T : class, ITrackable, new();
		T Update<T>(string connectionString, T entity, Node node) where T : class, ITrackable, new();
		IEnumerable<T> Select<T>(string connectionString, Node node, Expression<Func<T, bool>> whereClause) where T : class, new();
		IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters) where T : class, new();
		IEnumerable<dynamic> Select(string connectionString, string rawQuery, IDictionary<string, object> parameters);
		string GetSqlDbType(Type csType);
	}
}