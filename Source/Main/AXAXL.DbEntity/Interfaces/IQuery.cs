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
		/// <param name="whereClause">Lambda expresson that returns boolean.</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Where(Expression<Func<T, bool>> whereClause);
		/// <summary>
		/// Specify additional where clause.  This and the where clause from <see cref="Where(Expression{Func{T, bool}})"/>
		/// will be connected with an "AND" operator.
		/// </summary>
		/// <param name="whereClause">Lambda expression that returns boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> And(Expression<Func<T, bool>> whereClause);
		/// <summary>
		/// A group of conditions evaluated together by an "OR" operator.  This and other conditions supplied by
		/// <see cref="Where(Expression{Func{T, bool}})"/> and <see cref="And(Expression{Func{T, bool}})"/> will be evaluated together
		/// by "AND" sql operators.
		/// </summary>
		/// <param name="orClauses">a list of Lambda expressions which will return boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Or(params Expression<Func<T, bool>>[] orClauses);
		/// <summary>
		/// Excluding the childset by naming them in the Lambda expression.
		/// </summary>
		/// <param name="exclusions">Lambda expression identifying a childset reference on class type <typeparamref name="T"/></param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions);
		/// <summary>
		/// Order resultset according to the specified property and ordering
		/// </summary>
		/// <param name="property">Lambda expression that specify an entity property</param>
		/// <param name="isAscending">Order. Assume ascending</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> OrderBy(Expression<Func<T, dynamic>> property, bool isAscending = true);
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
