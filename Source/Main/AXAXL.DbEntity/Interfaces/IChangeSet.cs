using System;
using System.Linq.Expressions;
using System.Transactions;
using System.Collections.Generic;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// This is an interface to group changes together with respect to transaction scope option (such as Required, RequiredNew, Suppress) and isolation level,
	/// like ReadCommitted, Serializable etc.
	/// </summary>
	public interface IChangeSet
	{
		/// <summary>
		/// Insert an array of entities into the database.  All are assumed to be new without regarding to the <seealso cref="ITrackable.EntityStatus"/> of each entity.
		/// </summary>
		/// <param name="entities">array of entities</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet Insert(params ITrackable[] entities);
		/// <summary>
		/// Update an array of entities in the database.  All are assumed to being updated without regarding to the <seealso cref="ITrackable.EntityStatus"/> of each entity.
		/// </summary>
		/// <param name="entities">array of entities</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet Update(params ITrackable[] entities);
		/// <summary>
		/// Delete an array of entities from the database.  All are assumed to be deleted without regarding to the <seealso cref="ITrackable.EntityStatus"/> of each entity.
		/// </summary>
		/// <param name="entities">array of entities</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet Delete(params ITrackable[] entities);
		/// <summary>
		/// Submit an array of entities to be saved.  No change made to database yet.  What needs to be done depends on the <seealso cref="ITrackable.EntityStatus"/> of each entity.
		/// </summary>
		/// <param name="entities">array of entity</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet Save(params ITrackable[] entities);
		/// <summary>
		/// Assign transaction scope to this change set. If none is assigned, change set will use the root transaction scope specified in <see cref="IPersist"/>
		/// </summary>
		/// <param name="option">transaction scope option</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet SetTransactionScopeOption(TransactionScopeOption option);
		/// <summary>
		/// Assign isolation level to this change set. If none is assigned, change set will use the root transaction isolation level specified in <see cref="IPersist"/>
		/// </summary>
		/// <param name="isolationLevel"></param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet SetIsolationLevel(IsolationLevel isolationLevel);
		/// <summary>
		/// To skip childsets.  Childset of the skipped childset will not be included in this change set also.
		/// </summary>
		/// <typeparam name="TObject">Entity object</typeparam>
		/// <param name="exclusions">Lambda express which idenntify a property in <typeparamref name="TObject"/> that is referencing a childset.</param>
		/// <returns>Return itself for chaining method calls.</returns>
		IChangeSet Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class;
		/// <summary>
		/// Return transaction scope assigned.  
		/// Be reminded that it can be just the default value of enum <see cref="TransactionScopeOption"/> if <see cref="SetTransactionScopeOption(TransactionScopeOption)"/> is not called.
		/// Must check <see cref="IsTransactionScopeOptionChanged"/> to see if <see cref="SetTransactionScopeOption(TransactionScopeOption)"/> is called.
		/// </summary>
		TransactionScopeOption ScopeOption { get; }
		/// <summary>
		/// Return isolation level assigned.
		/// Be reminded that it can be just the default value of enum <see cref="IsolationLevel"/> if <see cref="SetIsolationLevel(IsolationLevel)"/> has not been used.
		/// Must check <see cref="IsIsolationLevelChanged"/> to see if <see cref="SetIsolationLevel(IsolationLevel)"/> is called.
		/// </summary>
		IsolationLevel Isolation { get; }
		/// <summary>
		/// Compiled version of <see cref="Exclude{TObject}(Expression{Func{TObject, dynamic}}[])"/> which identify which <see cref="Node"/> and from such which <see cref="NodeProperty"/> is identified as excluded.
		/// </summary>
		IDictionary<Node, NodeProperty[]> Exclusion { get; }
		/// <summary>
		/// Return all entities submitted to this change set via <see cref="Save(ITrackable[])"/>
		/// </summary>
		IList<ITrackable> Changes { get; }
		/// <summary>
		/// Return true if <see cref="SetTransactionScopeOption(TransactionScopeOption)"/> has been called to setup <see cref="TransactionScopeOption"/> for this change set.
		/// </summary>
		bool IsTransactionScopeOptionChanged { get;  }
		/// <summary>
		/// Return true if <see cref="SetIsolationLevel(IsolationLevel)"/> has been called to assigne <see cref="IsolationLevel"/> to this change set.
		/// </summary>
		bool IsIsolationLevelChanged { get; }

	}
}
