using System;
using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IPersist
	{
		IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet);
		IPersist SetRootTransactionSCopeOption(TransactionScopeOption scopeOption);
		IPersist SetRootIsolationLevel(IsolationLevel isolation);

		int Commit();
	}
}
