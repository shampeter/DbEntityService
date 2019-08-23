using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbServiceOption
	{
		IDbServiceOption AddOrUpdateConnection(string connectionName, string connectionString);
		IDbServiceOption SetAsDefaultConnection(string connectionName);
		string GetDefaultConnectionString();
		string GetConnectionString(string connectionName);
		IDbServiceOption SetRootDefaultTransactionScope(TransactionScopeOption scope);
		IDbServiceOption SetRootDefaultIsolation(IsolationLevel isolation);
		bool IsRootDefaultTransactionScopeChanged { get; }
		bool IsRootDefaultIsolationLevelChanged { get; }
	}
}
