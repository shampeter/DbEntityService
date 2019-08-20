using System.Diagnostics;
using System.Collections.Generic;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.Services
{
	public class DbServiceOption : IDbServiceOption
	{
		private IDictionary<string, string> ConnectionMap { get; set; }
		private string DefaultConnectionName { get; set; }
		public DbServiceOption()
		{
			this.ConnectionMap = new Dictionary<string, string>();
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
	}
}
