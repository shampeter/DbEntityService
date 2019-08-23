using System;
using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// This interface helps group different changes with respect to transaction scope option and isolation level into one transaction.
	/// </summary>
	public interface IPersist
	{
		/// <summary>
		/// Submit a change set.  No update to database yet.
		/// </summary>
		/// <param name="submitChangeSet">A change set</param>
		/// <returns>Returns itself for chaining method calls</returns>
		IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet);
		/// <summary>
		/// Setup transaction scope option for the root transaction.  If none is specified by this method, transaction will be defaulted from <see cref="IDbServiceOption"/>.
		/// Change set will use this as default if none is specified.
		/// </summary>
		/// <param name="scopeOption">Transaction scope option</param>
		/// <returns>Returns itself for chaining method calls</returns>
		IPersist SetRootTransactionSCopeOption(TransactionScopeOption scopeOption);
		/// <summary>
		/// Setup root transaction isolation level. If none is specified by this method, isolation level will be defaulted from <see cref="IDbServiceOption"/>.
		/// Change set will use this as default if none is specified.
		/// </summary>
		/// <param name="isolation">Isolation level</param>
		/// <returns>Return itself for chaining method calls</returns>
		IPersist SetRootIsolationLevel(IsolationLevel isolation);
		/// <summary>
		/// Commit all changes submitted so far into database, then follow by a transaction complete.
		/// </summary>
		/// <returns>No of record changes, including inserted, updated and deleted</returns>
		int Commit();
	}
}
