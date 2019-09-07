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
		/// Boostrap DbService where the servie will scan <paramref name="assemblies"/> for class type which has the a <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/> defined.
		/// and create <see cref="AXAXL.DbEntity.EntityGraph.Node"/> to store the discovered meta data.
		/// if <paramref name="assemblies"/> is null or empty, then use all loaded assemblies as found in <see cref="AppDomain.CurrentDomain"/>.
		/// Use <paramref name="assemblyNamePrefixes"/> to narrow down the assembly to start by specifying the assembly name prefix.
		/// Service will use case insensitives match to test assembly by name with <see cref="string.StartsWith(string, StringComparison)"/>
		/// If <paramref name="assemblyNamePrefixes"/> is null or empty, service will search through all assemblies specified.
		/// </summary>
		/// <param name="assemblies">list of assemblies to scan.  If none, full assemblies loaded will be used. <see cref="AppDomain.CurrentDomain"/></param>
		/// <param name="assemblyNamePrefixes">Assembely name prefixes.</param>
		/// <returns></returns>
		IDbService Bootstrap(Assembly[] assemblies = null, string[] assemblyNamePrefixes = null);
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
		IExecuteCommand ExecuteCommand();
		/*
		 * Moved functionality to IExecuteCommand which combine running stored procedure, raw sql such as insert, update, and delete into one.
		IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null);
		*/
	}
}
