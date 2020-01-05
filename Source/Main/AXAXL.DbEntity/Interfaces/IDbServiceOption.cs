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
		/// <summary>
		/// Return the number of processor assigned by <see cref="SetProcessorCount(int)"/>.  If not assigned, <see cref="System.Environment.ProcessorCount"/> will be used.
		/// </summary>
		/// <returns>Int.  Number of processor in the environment.</returns>
		int GetProcessorCount();
		/// <summary>
		/// Assign the default root transaction scope for <see cref="IPersist"/>.  If this method is not used to setup default, RequiredNew will be used.
		/// </summary>
		/// <param name="scope">Transaction scope option.  See <see cref="TransactionScopeOption"/></param>
		/// <returns>Return itself to enable method chaining.</returns>
		IDbServiceOption SetRootDefaultTransactionScope(TransactionScopeOption scope);
		/// <summary>
		/// Assign default isolation level for <see cref="IPersist"/>.  If this method is not called to setup default, ReadCommitted will be used.
		/// </summary>
		/// <param name="isolation">Isolation level. see <see cref="IsolationLevel"/></param>
		/// <returns>Return itself to enable method chaining.</returns>
		IDbServiceOption SetRootDefaultIsolation(IsolationLevel isolation);
		/// <summary>
		/// Assign number of processor available in the environment.  By default, this number will be the <see cref="System.Environment.ProcessorCount"/>.
		/// </summary>
		/// <param name="processorCount">Int. Number of processor.</param>
		/// <returns>Return itself to enable method chaining.</returns>
		IDbServiceOption SetProcessorCount(int processorCount);
		/// <summary>
		/// Assign query batch size.  After version 1.3.1, query is capable to retrieve child entities of the same type for multiple parent entities, thus saving
		/// multiple round trips of retrieving children of the same type for multiple parents.  Foreign keys will be submitted to operatiors, such as IN, to retrieve
		/// child set in one go.  The limit is the number of SQL parameters allowed in one SQL.  Thus this batch size comes into picture.
		/// A batch size of 1000 means 1000 parents will be grouped together in retrieving their children.  Thus if there is only 1 primary key for such entity type,
		/// the number of SQL parameters will be 1000.  Hence if the primary key length is 2 for a parent entity type, the number of parents grouped together will be
		/// 500.  The current limit of SQL parameters in one query is 2100.
		/// </summary>
		/// <param name="queryBatchSize">Number of parameters in one query for retrieving children for multiple parents.</param>
		/// <returns>Return itself to enable method chaining.</returns>
		IDbServiceOption SetQueryBatchSize(int queryBatchSize);
		IDbServiceOption PrintNodeMapToFile(string filename);
		TransactionScopeOption RootDefaultTransactionScope { get; }
		IsolationLevel RootDefaultIsolation { get; }
		string NodeMapPrintFilename { get; }
		int QueryBatchSize { get; }
	}
}
