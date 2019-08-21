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
		private IDictionary<Node, NodeProperty[]> Exclusion { get; set; }
		private IDatabaseDriver Driver { get; set; }
		private IList<(ITrackable[] Entities, TransactionScopeOption ScopeOption, IsolationLevel Level)> ChangeSets { get; set; }
		public Persist(ILogger log, IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver)
		{
			this.ServiceOption = serviceOption;
			this.Log = log;
			this.NodeMap = nodeMap;
			this.Exclusion = new Dictionary<Node, NodeProperty[]>();
			this.Driver = driver;
			this.ChangeSets = new List<(ITrackable[] Entities, TransactionScopeOption ScopeOption, IsolationLevel Level)>();
		}
		public int Commit()
		{
			throw new NotImplementedException();
		}

		public IPersist Save(ITrackable entity, TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
		{
			this.ChangeSets.Add((new[] { entity }, transactionScopeOption, isolationLevel));
			return this;
		}

		public IPersist Save(IEnumerable<ITrackable> entities, TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
		{
			this.ChangeSets.Add((entities.ToArray(), transactionScopeOption, isolationLevel));
			return this;
		}

		public IPersist Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new()
		{
			var node = this.NodeMap.GetNode(typeof(TObject));
			var excludedProperties = this.IdentifyMembers<TObject>(node, exclusions);
			if (excludedProperties.Length > 0)
			{
				this.Exclusion.Add(node, excludedProperties);
			}
			return this;
		}
		private NodeProperty[] IdentifyMembers<TEntity>(Node node, params Expression<Func<TEntity, dynamic>>[] memberExpressions) where TEntity : class
		{
			if (memberExpressions == null || memberExpressions.Length <= 0) return new NodeProperty[0];
			return memberExpressions.Select(e => node.IdentifyMember<TEntity>(e)).ToArray();
		}
	}
}
