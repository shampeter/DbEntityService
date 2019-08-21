using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbService
	{
		IDbService Config(Action<IDbServiceOption> config);
		IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null);
		IQuery<T> Query<T>() where T : class, new();
		T Persist<T>(T entity) where T : class, ITrackable, new();
	}
}
