using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDatabaseDriver
	{
		T Insert<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		T Delete<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		T Update<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		IEnumerable<T> Select<T>(string connectionString, Node node, Expression<Func<T, bool>> whereClause, int timeoutDurationInSeconds = 30) where T : class, new();
		IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters, int timeoutDurationInSeconds = 30) where T : class, new();
		IEnumerable<dynamic> Select(string connectionString, string rawQuery, IDictionary<string, object> parameters, int timeoutDurationInSeconds = 30);
		string GetSqlDbType(Type csType);
	}
}