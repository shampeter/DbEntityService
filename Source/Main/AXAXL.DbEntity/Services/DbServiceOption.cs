using System.Diagnostics;
using System.Collections.Generic;
using AXAXL.DbEntity.Interfaces;
using System.Transactions;

namespace AXAXL.DbEntity.Services
{
	public class DbServiceOption : IDbServiceOption
	{
		private IDictionary<string, string> ConnectionMap { get; set; }
		private string DefaultConnectionName { get; set; }
		public TransactionScopeOption RootDefaultTransactionScope { get; private set; }
		public IsolationLevel RootDefaultIsolation { get; private set; }
		public string NodeMapPrintFilename { get; private set; }
		public DbServiceOption()
		{
			this.ConnectionMap = new Dictionary<string, string>();
			this.RootDefaultTransactionScope = TransactionScopeOption.Required;
			this.RootDefaultIsolation = IsolationLevel.ReadCommitted;
		}
		public IDbServiceOption AddOrUpdateConnection(string connectionName, string connectionString)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionName) == false);
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false);

			if (this.ConnectionMap.TryAdd(connectionName, connectionString))
			{
				this.ConnectionMap[connectionName] = connectionString;
			}
			return this;
		}

		public IDbServiceOption SetAsDefaultConnection(string connectionName)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionName) == false);
			Debug.Assert(this.ConnectionMap.ContainsKey(connectionName) == true, $"No connection setup by name '{connectionName}' found");
			this.DefaultConnectionName = connectionName;
			return this;
		}
		public IDbServiceOption PrintNodeMapToFile(string filename)
		{
			this.NodeMapPrintFilename = filename;
			return this;
		}
		public string GetDefaultConnectionString()
		{
			Debug.Assert(string.IsNullOrEmpty(this.DefaultConnectionName) == false);
			Debug.Assert(this.ConnectionMap.ContainsKey(this.DefaultConnectionName));

			return this.ConnectionMap[this.DefaultConnectionName];
		}
		public string GetConnectionString(string connectionName)
		{
			Debug.Assert(string.IsNullOrEmpty(connectionName) == false);
			Debug.Assert(this.ConnectionMap.ContainsKey(connectionName) == true, $"No connection setup by name '{connectionName}' found");

			return this.ConnectionMap[connectionName];
		}

		public IDbServiceOption SetRootDefaultTransactionScope(TransactionScopeOption scope)
		{
			this.RootDefaultTransactionScope = scope;
			return this;
		}

		public IDbServiceOption SetRootDefaultIsolation(IsolationLevel isolation)
		{
			this.RootDefaultIsolation = isolation;
			return this;
		}
	}
}
