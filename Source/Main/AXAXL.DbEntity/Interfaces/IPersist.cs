using System;
using System.Linq.Expressions;
using System.Transactions;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IPersist
	{
		IPersist Save(ITrackable entity, TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
		IPersist Save(IEnumerable<ITrackable> entities, TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
		IPersist Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new();
		int Commit();
	}
}
