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
			this.scopeOption = serviceOption.RootDefaultTransactionScope;
			this.isolation = serviceOption.RootDefaultIsolation;
		}
		public int Commit()
		{
			var rowCount = 0;
			var rootOption = new TransactionOptions { IsolationLevel = this.isolation };
			// TODO: Need fine tuning on transaction behavior.
			using (var rootTransaction = new TransactionScope(this.scopeOption, rootOption))
			{
				foreach(var changeSet in this.ChangeSets)
				{
					if (changeSet.IsIsolationLevelChanged || changeSet.IsTransactionScopeOptionChanged)
					{
						var isolation = changeSet.IsIsolationLevelChanged == true ? changeSet.Isolation : this.isolation;
						var scope = changeSet.IsTransactionScopeOptionChanged == true ? changeSet.ScopeOption : TransactionScopeOption.Required;
						var option = new TransactionOptions { IsolationLevel = isolation };
						using (var changeSetTransaction = new TransactionScope(scope, option))
						{
							rowCount += this.CommitChangeSet(changeSet);
							changeSetTransaction.Complete();
						}
					}
					else
					{
						rowCount += this.CommitChangeSet(changeSet);
					}
				}
				rootTransaction.Complete();
			}

			return rowCount;
		}

		private int CommitChangeSet(IChangeSet changeSet)
		{
			int rowCount = 0;
			foreach (var eachEntity in changeSet.Changes)
			{
				var director = new Director(this.ServiceOption, this.NodeMap, this.Driver, this.Log, changeSet.Exclusion);
				rowCount = director.Save(eachEntity);
			}

			return rowCount;
		}

		public IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet)
		{
			Debug.Assert(submitChangeSet != null);

			IChangeSet set = new ChangeSet(this.Log, this.NodeMap);
			set = submitChangeSet.Invoke(set);
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
