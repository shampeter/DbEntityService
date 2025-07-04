﻿using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace AXAXL.DbEntity.Interfaces
{
	public enum RetrievalStrategies
	{
		OneEntityAtATimeInParallel,
		OneEntityAtATimeInSequence,
		AllEntitiesAtOnce
	}
	public interface IQuery<T> where T: class, new()
	{
		/// <summary>
		/// Execute the query and return entities found in a list.
		/// </summary>
		/// <param name="maxNumOfRow">Set the maximum number of rows to be returned.  If set as <= 0, returns all rows</param>
		/// <param name="strategy">Strategy to build the entity tree.</param>
		/// <returns>List of entities</returns>
		IList<T> ToList(int maxNumOfRow = -1, RetrievalStrategies strategy = RetrievalStrategies.AllEntitiesAtOnce);
		/// <summary>
		/// Execute the query and return entities found in an array.
		/// </summary>
		/// <param name="maxNumOfRow">Set the maximum number of rows to be returned.  If set as <= 0, returns all rows</param>
		/// <param name="strategy">Strategy to build the entity tree.</param>
		/// <returns>An array of entities</returns>
		T[] ToArray(int maxNumOfRow = -1, RetrievalStrategies strategy = RetrievalStrategies.AllEntitiesAtOnce);
		/// <summary>
		/// Specify the where clause of the query by a Lambda expression.
		/// </summary>
		/// <param name="whereClause">Lambda expresson that returns boolean.</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Where(Expression<Func<T, bool>> whereClause);
		/// <summary>
		/// Specify where clause for child set filtering.  Inner join between <typeparamref name="TParent"/> and <typeparamref name="TChild"/>.
		/// </summary>
		/// <typeparam name="TParent">Parent entity type.  Together with <typeparamref name="TChild"/> type helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <typeparam name="TChild">Child entity type.  Together with <typeparamref name="TParent"/> helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <param name="whereClause">Lambda expresson that returns boolean.</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> InnerJoin<TParent, TChild>(Expression<Func<TChild, bool>> whereClause);
		/// <summary>
		/// Specify where clause for child set filtering.  Left outer join between <typeparamref name="TParent"/> and <typeparamref name="TChild"/>
		/// </summary>
		/// <typeparam name="TParent">Parent entity type.  Together with <typeparamref name="TChild"/> type helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <typeparam name="TChild">Child entity type.  Together with <typeparamref name="TParent"/> helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <param name="whereClause">Lambda expresson that returns boolean.</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> LeftOuterJoin<TParent, TChild>(Expression<Func<TChild, bool>> whereClause);
		/// <summary>
		/// Specify additional where clause.  This and the where clause from <see cref="Where(Expression{Func{T, bool}})"/>
		/// will be connected with an "AND" operator.
		/// </summary>
		/// <param name="whereClause">Lambda expression that returns boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> And(Expression<Func<T, bool>> whereClause);
		/* Removed API cause it is too confusiong.
		 * 
		/// <summary>
		/// Specify where clause for child set filtering
		/// </summary>
		/// <typeparam name="TParent">Parent entity type.  Together with <typeparamref name="TChild"/> type helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <typeparam name="TChild">Child entity type.  Together with <typeparamref name="TParent"/> helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <param name="whereClause">Lambda expresson that returns boolean.</param>
		/// <param name="isOuterJoin">True if join between <typeparamref name="TParent"/> and <typeparamref name="TChild"/> should be a Left Outer Join.</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> And<TParent, TChild>(Expression<Func<TChild, bool>> whereClause, bool isOuterJoin = true);
		*/
		/// <summary>
		/// A group of conditions evaluated together by an "OR" operator.  This and other conditions supplied by
		/// <see cref="Where(Expression{Func{T, bool}})"/> and <see cref="And(Expression{Func{T, bool}})"/> will be evaluated together
		/// by "AND" sql operators.
		/// </summary>
		/// <param name="orClauses">a list of Lambda expressions which will return boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> Or(params Expression<Func<T, bool>>[] orClauses);
		/// <summary>
		/// A group of conditions evaluated together by an "OR" operator.  This and other conditions supplied by
		/// <see cref="Where(Expression{Func{T, bool}})"/> and <see cref="And(Expression{Func{T, bool}})"/> will be evaluated together
		/// by "AND" sql operators.
		/// Left out join is assumed between <typeparamref name="TParent"/> and <typeparamref name="TChild"/>.
		/// </summary>
		/// <example>
		/// For example, in a parent -> child -> grand-child relationship.  The following code
		/// <code>
		/// LeftOuterJoinOr<child, grand-child>( grandchild -> grandchild.SomeProp1 = SomeValue1, grandchild.SomeProp2 == SomeValue2)
		/// </code>
		/// will be executed as
		/// <code>
		/// FROM parent t0
		/// LEFT OUTER JOIN child t1 on t0.primary_key = t1.forign_key
		/// LEFT OUTER JOIN grant-child t2 on t1.primary_key = t2.foreign_key
		/// WHERE t2.some_prop1 = somevalue1 or t2.some_prop2 = somevalue2
		/// </code>
		/// </example>
		/// <typeparam name="TParent">Parent entity type.  Together with <typeparamref name="TChild"/> type helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <typeparam name="TChild">Child entity type.  Together with <typeparamref name="TParent"/> helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <param name="orClauses">a list of Lambda expressions which will return boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> LeftOuterJoinOr<TParent, TChild>(params Expression<Func<TChild, bool>>[] orClauses);
		/// <summary>
		/// A group of conditions evaluated together by an "OR" operator.  This and other conditions supplied by
		/// <see cref="Where(Expression{Func{T, bool}})"/> and <see cref="And(Expression{Func{T, bool}})"/> will be evaluated together
		/// by "AND" sql operators.
		/// </summary>
		/// <example>
		/// For example, in a parent -> child -> grand-child relationship.  The following code
		/// <code>
		/// InnerJoinOr<child, grand-child>( grandchild -> grandchild.SomeProp1 = SomeValue1, grandchild.SomeProp2 == SomeValue2)
		/// </code>
		/// will be executed as
		/// <code>
		/// FROM parent t0
		/// INNER JOIN child t1 on t0.primary_key = t1.forign_key
		/// INNER JOIN grant-child t2 on t1.primary_key = t2.foreign_key
		/// WHERE t2.some_prop1 = somevalue1 or t2.some_prop2 = somevalue2
		/// </code>
		/// </example>
		/// <typeparam name="TParent">Parent entity type.  Together with <typeparamref name="TChild"/> type helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <typeparam name="TChild">Child entity type.  Together with <typeparamref name="TParent"/> helps identify the parent-child relation that this where clause is going to filter</typeparam>
		/// <param name="orClauses">a list of Lambda expressions which will return boolean</param>
		/// <returns>Return itself for method call chaining.</returns>
		IQuery<T> InnerJoinOr<TParent, TChild>(params Expression<Func<TChild, bool>>[] orClauses);
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
