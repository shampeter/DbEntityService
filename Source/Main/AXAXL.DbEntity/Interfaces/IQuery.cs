using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IQuery<T> where T: class, new()
	{
		/// <summary>
		/// Execute the query and return entities found in a list.
		/// </summary>
		/// <param name="maxNumOfRow">Set the maximum number of rows to be returned.  If set as <= 0, returns all rows</param>
		/// <returns>List of entities</returns>
		IList<T> ToList(int maxNumOfRow = -1);
		/// <summary>
		/// Execute the query and return entities found in an array.
		/// </summary>
		/// <param name="maxNumOfRow">Set the maximum number of rows to be returned.  If set as <= 0, returns all rows</param>
		/// <returns>An array of entities</returns>
		T[] ToArray(int maxNumOfRow = -1);
		/// <summary>
		/// Specify the where clause of the query by a Lambda expression.
		/// </summary>
		/// <param name="whereClause"></param>
		/// <returns></returns>
		IQuery<T> Where(Expression<Func<T, bool>> whereClause);
		/// <summary>
		/// Excluding the childset by naming them in the Lambda expression.
		/// </summary>
		/// <param name="exclusions">Lambda expression identifying a childset reference on class type <typeparamref name="T"/></param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions);
		/// <summary>
		/// Assign query timeout time.  If none is specified, default is 30 seconds,
		/// according to <![CDATA[https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout?view=netcore-2.2.]]>
		/// </summary>
		/// <param name="timeoutDurationInSeconds">Timeout in seconds</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> SetTimeout(int timeoutDurationInSeconds = 30);
		/// <summary>
		/// Excluding the childset by naming them in the Lambda expression.
		/// </summary>
		/// <typeparam name="TObject">Childset entity class type other than <typeparamref name="T"/></typeparam>
		/// <param name="exclusions">Lambda expression identifying a childset reference on class type <typeparamref name="TObject"/></param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new();
	}
}
