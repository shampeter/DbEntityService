﻿using System;
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
		public DbService(ILoggerFactory factory, IDatabaseDriver dbDriver, INodeMap nodeMap, IDbServiceOption serviceOption)
		{
			Debug.Assert(factory != null);
			Debug.Assert(dbDriver != null);
			Debug.Assert(nodeMap != null);
			Debug.Assert(serviceOption != null);

			this.Log = factory.CreateLogger<DbService>();
			this.ServiceOption = serviceOption;
			this.Driver = dbDriver;
			this.NodeMap = nodeMap;
		}
		public IDbService Bootstrap(Assembly[] assemblies = null, string[] assemblyNamePrefixes = null)
		{
			this.NodeMap.BuildNodes(assemblies, assemblyNamePrefixes, this.ServiceOption.NodeMapPrintFilename);
			return this;
		}

		public IQuery<T> Query<T>() where T : class, new()
		{
			return new DbQuery<T>(this.Log, this.ServiceOption, this.NodeMap, this.Driver);
		}

		public IPersist Persist()
		{
			var unitOfWork = new Persist(this.Log, this.ServiceOption, this.NodeMap, this.Driver);
			unitOfWork
				.SetRootTransactionSCopeOption(this.ServiceOption.RootDefaultTransactionScope)
				.SetRootIsolationLevel(this.ServiceOption.RootDefaultIsolation)
				;
			return unitOfWork;
		}

		public IExecuteCommand ExecuteCommand()
		{
			return new ExecuteCommand(this.Log, this.ServiceOption, this.NodeMap, this.Driver);
		}
		/* Functionality has moved to IExecuteCommand
		 * 
		public IEnumerable<dynamic> FromRawSql(string rawQuery, IDictionary<string, object> parameters, string connectionName = null)
		{
			Debug.Assert(string.IsNullOrEmpty(rawQuery) == false);
			Debug.Assert(this.Driver != null);

			var connection = string.IsNullOrEmpty(connectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(connectionName);
			return this.Driver.Select(connection, rawQuery, parameters);
		}
		*/
	}
}
