using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// Server Option interface which define funcation to specify runtime database context such as connection string.
	/// </summary>
	public interface IDbServiceOption
	{
		/// <summary>
		/// Assign or update a connection name with a connection string.
		/// </summary>
		/// <param name="connectionName">Name of connection</param>
		/// <param name="connectionString">Database connection string</param>
		/// <returns></returns>
		IDbServiceOption AddOrUpdateConnection(string connectionName, string connectionString);
		/// <summary>
		/// Assign a connection name as a default.  Connection name is assigned to an entity object via <see cref="AXAXL.DbEntity.Annotation.ConnectionAttribute"/>.  If none is specified on an entity object,
		/// this default will be used.
		/// </summary>
		/// <param name="connectionName"></param>
		/// <returns></returns>
		IDbServiceOption SetAsDefaultConnection(string connectionName);
		/// <summary>
		/// Return default connection string.
		/// </summary>
		/// <returns></returns>
		string GetDefaultConnectionString();
		/// <summary>
		/// Return connection string by the specified connection name <paramref name="connectionName"/>.
		/// </summary>
		/// <param name="connectionName">Name for identifying a connection string as assigned in <see cref="AddOrUpdateConnection(string, string)"/></param>
		/// <returns>Database connection string.</returns>
		string GetConnectionString(string connectionName);
		IDbServiceOption SetRootDefaultTransactionScope(TransactionScopeOption scope);
		IDbServiceOption SetRootDefaultIsolation(IsolationLevel isolation);
		bool IsRootDefaultTransactionScopeChanged { get; }
		bool IsRootDefaultIsolationLevelChanged { get; }
	}
}
