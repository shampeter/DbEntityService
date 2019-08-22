using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbService
	{
		/// <summary>
		/// Boostrap DbService where the servie will scan <paramref name="assemblies"/> for class type which has the a <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/> defined
		/// and create <see cref="AXAXL.DbEntity.EntityGraph.Node"/> to store the discovered meta data.
		/// </summary>
		/// <param name="assemblies">list of assemblies to scan.  If none, full assemblies loaded will be used. <see cref="AppDomain.CurrentDomain"/></param>
		/// <returns></returns>
		IDbService Bootstrap(params Assembly[] assemblies);
		/// <summary>
		/// Run raw sql query.  See <see cref="IDatabaseDriver"/> for details.
		/// </summary>
		/// <param name="rawQuery"></param>
		/// <param name="parameters"></param>
		/// <param name="connectionName"></param>
		/// <returns></returns>
		IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null);
		/// <summary>
		/// Obtain an instance of IQuery to execute query on entity of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Entity class type</typeparam>
		/// <returns>Return itself for chaining method calls.</returns>
		IQuery<T> Query<T>() where T : class, new();
		/// <summary>
		/// Obtain an IPersist for saving entity objects as a unit.
		/// </summary>
		/// <returns>Return itself for chaining method calls.</returns>
		IPersist Persist();
	}
}
