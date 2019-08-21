using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.Extensions;

namespace AXAXL.DbEntity.Services
{
	public class Director
	{
		private IDatabaseDriver Driver { get; set; }
		private INodeMap NodeMap { get; set; }
		private IDictionary<Node, NodeProperty[]> Exclusion { get; set; }
		private ILogger Log { get; set; }
		private IDbServiceOption ServiceOption { get; set; }
		private int TimeoutDurationInSeconds { get; set; }
		private IList<ITrackable> DeleteQueue { get; set; }
		//private ISet<string> PathWalked { get; set; }
		public Director(IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver, ILogger log, IDictionary<Node, NodeProperty[]> exclusion, int timeoutDurationInSeconds = 30)
		{
			this.NodeMap = nodeMap;
			this.Driver = driver;
			this.Log = log;
			this.Exclusion = exclusion ?? new Dictionary<Node, NodeProperty[]>();
			this.ServiceOption = serviceOption;
			this.TimeoutDurationInSeconds = timeoutDurationInSeconds;
			this.DeleteQueue = new List<ITrackable>();
			//this.PathWalked = new HashSet<string>();
		}
		// TODO: Need to double check the build logic
		public T Build<T>(T entity, bool isMovingTowardsParent, bool isMovingTowardsChild) where T : class, new()
		{
			Node node = this.NodeMap.GetNode(entity.GetType());

			if (isMovingTowardsChild)
			{
				foreach (var eachParentToChild in node.AllChildEdgeNames())
				{
					var edge = node.GetEdgeToChildren(eachParentToChild);

/* Doesn't quite work this way for recording the path because the same path can be walked by different node of different record.					
 					var edgeSignature = this.GetEdgeSignature(edge);
					// child path has been walked.
					if (this.PathWalked.Contains(edgeSignature)) continue;
					// if not, remember this path in order to prevent going in cycles.
					this.PathWalked.Add(edgeSignature);
*/
					// child path excluded
					if (this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ChildReferenceOnParentNode)) continue;

					var readers = edge.ParentPrimaryKeyReaders;
					IDictionary<string, object> childKeys = new Dictionary<string, object>();
					for (int i = 0; i < readers.Length && i < edge.ChildNodeForeignKeys.Length; i++)
					{
						childKeys.Add(edge.ChildNodeForeignKeys[i].PropertyName, readers[i](entity));
					}
					var connection = this.GetConnectionString(edge.ChildNode);
					var children = this.Driver.Select<object>(connection, edge.ChildNode, childKeys, this.TimeoutDurationInSeconds);
					edge.ChildAddingAction(entity, children);

					foreach (var eachChild in children)
					{
						edge.ParentSettingAction(eachChild, entity);
						this.Build(eachChild, true, true);
					}
				}
			}
			if (isMovingTowardsParent)
			{
				foreach (var eachChildToParent in node.AllParentEdgeNames())
				{
					var edge = node.GetEdgeToParent(eachChildToParent);

/* Doesn't work this way because the same path can be walked by diffent node of different record.					
 					var edgeSignature = this.GetEdgeSignature(edge);
					if (this.PathWalked.Contains(edgeSignature)) continue;
					this.PathWalked.Add(edgeSignature);
*/
					if (this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ParentReferenceOnChildNode)) continue;

					var readers = edge.ChildForeignKeyReaders;
					IDictionary<string, object> parentKeys = new Dictionary<string, object>();
					for (int i = 0; i < readers.Length && i < edge.ParentNodePrimaryKeys.Length; i++)
					{
						parentKeys.Add(edge.ParentNodePrimaryKeys[i].PropertyName, readers[i](entity));
					}
					var connection = this.GetConnectionString(edge.ParentNode);
					var parent = this.Driver.Select<object>(connection, edge.ParentNode, parentKeys, this.TimeoutDurationInSeconds).FirstOrDefault();
					edge.ParentSettingAction(entity, parent);
					edge.ChildAddingAction(parent, new[] { entity });
					this.Build(parent, true, false);
				}
			}
			return entity;
		}

		// TODO: Complete the save implementation.
		public int Save(ITrackable entity)
		{
			var node = this.NodeMap.GetNode(entity.GetType());
			Debug.Assert(node != null, $"Failed to locate node for entity of type '{entity.GetType().FullName}'");
			var connectionString = this.GetConnectionString(node);
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false);

			var childEdges = node.AllChildEdgeNames().Select(p => node.GetEdgeToChildren(p)).ToArray();
			var recordCount = 0;
			switch (entity.EntityStatus)
			{
				case EntityStatusEnum.New:
					this.Driver.Insert<ITrackable>(connectionString, entity, node);
					recordCount++;
					this.PopulateChildForeignKeys(entity, node, childEdges);
					break;
				case EntityStatusEnum.Updated:
					this.Driver.Update<ITrackable>(connectionString, entity, node);
					recordCount++;
					break;
				case EntityStatusEnum.Deleted:
					this.DeleteQueue.Add(entity);
					break;
			}
			foreach(var edge in childEdges)
			{
				var iterator = edge.ChildReferenceOnParentNode.GetEnumeratorFunc(entity);

				while (iterator.MoveNext())
				{
					var child = iterator.Current;
					this.Save(child);
				}
			}
			return -1;
		}
		private void PopulateChildForeignKeys(ITrackable parent, Node node, NodeEdge[] edges)
		{
			foreach (var edge in edges)
			{
				var primaryKeys = edge.ParentPrimaryKeyReaders.Select(r => r(parent)).ToArray();
				var foreignKeyWriters = edge.ChildForeignKeyWriter;
				var iterator = edge.ChildReferenceOnParentNode.GetEnumeratorFunc(parent);

				while (iterator.MoveNext())
				{
					var child = iterator.Current;
					for (int i = 0; i < primaryKeys.Length && i < foreignKeyWriters.Length; i++)
					{
						foreignKeyWriters[i](child, primaryKeys[i]);
					}
				}
			}
		}
		private string GetEdgeSignature(NodeEdge edge)
		{
			return $"{edge.ParentNode.FullName}.{edge.ChildReferenceOnParentNode?.PropertyName},{edge.ChildNode.FullName}.{edge.ParentReferenceOnChildNode?.PropertyName}";
		}
		private string GetConnectionString(Node node)
		{
			return string.IsNullOrEmpty(node.DbConnectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(node.DbConnectionName);
		}
		[Conditional("DEBUG")]
		private void LogExpression(Expression expression)
		{
			this.Log.LogDebug(expression.ToMarkDown());
		}
	}
}
