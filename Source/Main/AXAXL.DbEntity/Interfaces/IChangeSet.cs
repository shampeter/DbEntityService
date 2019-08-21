using System;
using System.Linq.Expressions;
using System.Transactions;
using System.Collections.Generic;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IChangeSet
	{
		IChangeSet Save(ITrackable entity);
		IChangeSet Save(IEnumerable<ITrackable> entities);
		IChangeSet SetTransactionScopeOption(TransactionScopeOption option);
		IChangeSet SetIsolationLevel(IsolationLevel isolationLevel);
		IChangeSet Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class;
		TransactionScopeOption ScopeOption { get; }
		IsolationLevel Isolation { get; }
		IDictionary<Node, NodeProperty[]> Exclusion { get; }
		IList<ITrackable> Changes { get; }

	}
}
