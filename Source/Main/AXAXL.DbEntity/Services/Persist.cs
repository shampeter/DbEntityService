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
		public Persist(ILogger log, IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver)
		{
			this.ServiceOption = serviceOption;
			this.Log = log;
			this.NodeMap = nodeMap;
			this.Driver = driver;
			this.ChangeSets = new List<IChangeSet>();
		}
		public int Commit()
		{
			var rowCount = 0;
			var rootOption = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

			using (var rootTransaction = new TransactionScope(TransactionScopeOption.RequiresNew, rootOption))
			{
				foreach(var changeSet in this.ChangeSets)
				{
					var option = new TransactionOptions { IsolationLevel = changeSet.Isolation };
					using (var changeSetTransaction = new TransactionScope(changeSet.ScopeOption, option))
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
	}
}
