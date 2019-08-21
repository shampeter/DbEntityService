using System;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.Services
{
	public class DbService : IDbService
	{
		protected IDbServiceOption ServiceOption { get; set; }
		protected IDatabaseDriver Driver { get; set; }
		private ILogger Log { get; set; }
		private INodeMap NodeMap { get; set; }
		public DbService(ILoggerFactory factory, IDatabaseDriver dbDriver, INodeMap nodeMap)
		{
			this.Log = factory.CreateLogger<DbService>();
			this.ServiceOption = new DbServiceOption();
			this.Driver = dbDriver;
			this.NodeMap = nodeMap;
		}
		public IDbService Config(Action<IDbServiceOption> config)
		{
			config(this.ServiceOption);
			return this;
		}
		public bool Bootstrap(params Assembly[] assemblies)
		{
			this.NodeMap.BuildNodes(assemblies);
			return true;
		}
		public IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null)
		{
			Debug.Assert(string.IsNullOrEmpty(rawQuery) == false);
			Debug.Assert(this.Driver != null);

			var connection = string.IsNullOrEmpty(connectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(connectionName);
			return this.Driver.Select(connection, rawQuery, parameters);
		}

		public IQuery<T> Query<T>() where T : class, new()
		{
			return new DbQuery<T>(this.Log, this.ServiceOption, this.NodeMap, this.Driver);
		}

		public IPersist Persist()
		{
			return new Persist(this.Log, this.ServiceOption, this.NodeMap, this.Driver);
		}
	}
}
