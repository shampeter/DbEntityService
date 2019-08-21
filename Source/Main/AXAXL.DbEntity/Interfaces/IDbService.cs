using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbService
	{
		IDbService Config(Action<IDbServiceOption> config);
		bool Bootstrap(params Assembly[] assemblies);
		IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null);
		IQuery<T> Query<T>() where T : class, new();
		IPersist Persist();
	}
}
