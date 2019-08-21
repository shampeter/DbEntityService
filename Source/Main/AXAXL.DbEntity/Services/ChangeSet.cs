using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Transactions;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.EntityGraph;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.Services
{
	public class ChangeSet : IChangeSet
	{
		public IsolationLevel Isolation { get; private set; }
		public TransactionScopeOption ScopeOption { get; private set; }
		public IDictionary<Node, NodeProperty[]> Exclusion { get; private set; }
		private ILogger Log { get; set; }
		private INodeMap NodeMap { get; set; }
		public IList<ITrackable> Changes { get; private set; }
		private bool isolationChanged;
		private bool scopeOptionChanged;
		internal ChangeSet(ILogger log, INodeMap nodeMap)
		{
			this.Log = log;
			this.NodeMap = nodeMap;
			this.Isolation = IsolationLevel.ReadCommitted;
			this.ScopeOption = TransactionScopeOption.Required;
			this.Changes = new List<ITrackable>();
			this.isolationChanged = false;
			this.scopeOptionChanged = false;

		}
		public IChangeSet Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class
		{
			Debug.Assert(exclusions != null);
			var node = this.NodeMap.GetNode(typeof(TObject));
			var excludedProperties = node.IdentifyMembers<TObject>(exclusions);
			if (excludedProperties.Length > 0)
			{
				this.Exclusion.Add(node, excludedProperties);
			}

			return this;
		}

		public IChangeSet Save(params ITrackable[] entities)
		{
			Debug.Assert(entities != null);
			foreach (var eachEntity in entities)
			{
				this.Changes.Add(eachEntity);
			}

			return this;
		}

		public IChangeSet SetIsolationLevel(IsolationLevel isolationLevel)
		{
			this.Isolation = isolationLevel;
			this.isolationChanged = true;
			return this;
		}

		public IChangeSet SetTransactionScopeOption(TransactionScopeOption option)
		{
			this.ScopeOption = option;
			this.scopeOptionChanged = true;
			return this;
		}
		public bool IsTransactionScopeOptionChanged => this.scopeOptionChanged;

		public bool IsIsolationLevelChanged => this.isolationChanged;


	}
}
