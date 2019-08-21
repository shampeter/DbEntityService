using System;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.EntityGraph;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.Services
{
	public class Persist : IPersist
	{
		private IDbServiceOption ServiceOption { get; set; }
		private ILogger Log { get; set; }
		private INodeMap NodeMap { get; set; }
		private IDatabaseDriver Driver { get; set; }
		private List<IChangeSet> ChangeSets { get; set; }
		private TransactionScopeOption scopeOption;
		private IsolationLevel isolation;
		internal Persist(ILogger log, IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver)
		{
			this.ServiceOption = serviceOption;
			this.Log = log;
			this.NodeMap = nodeMap;
			this.Driver = driver;
			this.ChangeSets = new List<IChangeSet>();
			this.scopeOption = TransactionScopeOption.RequiresNew;
			this.isolation = IsolationLevel.ReadCommitted;
		}
		public int Commit()
		{
			var rowCount = 0;
			var rootOption = new TransactionOptions { IsolationLevel = this.isolation };

			using (var rootTransaction = new TransactionScope(this.scopeOption, rootOption))
			{
				foreach(var changeSet in this.ChangeSets)
				{
					var isolation = changeSet.IsIsolationLevelChanged == true ? changeSet.Isolation : this.isolation;
					var scope = changeSet.IsTransactionScopeOptionChanged == true ? changeSet.ScopeOption : this.scopeOption;
					var option = new TransactionOptions { IsolationLevel = isolation };
					using (var changeSetTransaction = new TransactionScope(scope, option))
					{
						foreach(var eachEntity in changeSet.Changes)
						{
							var director = new Director(this.ServiceOption, this.NodeMap, this.Driver, this.Log, changeSet.Exclusion);
							rowCount += director.Save(eachEntity);
						}
					}
				}
				rootTransaction.Complete();
			}

			return rowCount;
		}

		public IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet)
		{
			IChangeSet set = new ChangeSet(this.Log, this.NodeMap);
			set = submitChangeSet(set);
			this.ChangeSets.Add(set);

			return this;
		}

		public IPersist SetRootTransactionSCopeOption(TransactionScopeOption scopeOption)
		{
			this.scopeOption = scopeOption;
			return this;
		}

		public IPersist SetRootIsolationLevel(IsolationLevel isolation)
		{
			this.isolation = isolation;
			return this;
		}
	}
}
