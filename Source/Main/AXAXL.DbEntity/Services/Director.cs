using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		private IList<(ITrackable ParentEntity, NodeEdge Edge, ITrackable ChildEntity, Node ChildNode)> DeleteQueue { get; set; }
		private ParallelOptions ParallelRetrievalOptions { get; set; }
		private RetrievalStrategies Strategy { get; set; }
		//private ISet<string> PathWalked { get; set; }
		public Director(IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver, ILogger log, IDictionary<Node, NodeProperty[]> exclusion, ParallelOptions parallelRetrievalOptions, int timeoutDurationInSeconds = 30, RetrievalStrategies strategy = RetrievalStrategies.AllEntitiesAtOnce)
		{
			this.NodeMap = nodeMap;
			this.Driver = driver;
			this.Log = log;
			this.Exclusion = exclusion ?? new Dictionary<Node, NodeProperty[]>();
			this.ServiceOption = serviceOption;
			this.TimeoutDurationInSeconds = timeoutDurationInSeconds;
			this.DeleteQueue = new List<(ITrackable ParentEntity, NodeEdge Edge, ITrackable ChildEntity, Node ChildNode)>();
			this.ParallelRetrievalOptions = parallelRetrievalOptions;
			this.Strategy = strategy;
			//this.PathWalked = new HashSet<string>();
		}
		
		public IEnumerable<T> Build<T>(
			IEnumerable<T> entities,
			bool isMovingTowardsParent,
			bool isMovingTowardsChild,
			IEnumerable<ValueTuple<NodeEdge, Expression>> childWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childOrClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			) where T : class, new()
		{
			if (entities == null || entities.Count() <= 0) return entities;

			Node node = this.NodeMap.GetNode(entities.FirstOrDefault()?.GetType());

			if (isMovingTowardsChild)
			{
				var entityIndexes = new Dictionary<object[], int>(new ObjectArrayComparer());
				var primaryKeyCounts = node.PrimaryKeys.Keys.Count;
				var primaryKeyValues = node.PrimaryKeys.Keys.Select(k => new List<object>()).ToArray();
				for (int i = 0; i < entities.Count(); i++)
				{
					var keyValues = node.PrimaryKeyReaders.Select(r => r.Invoke(entities.ElementAt(i))).ToArray();
					// use primary key values as dictionary key to lookup th entity in the entity list.
					entityIndexes[keyValues] = i;
					// pust primary key values into a list.  For example, if the primary key is a compound key with 2 columns, c1 and c2, and there are 10 entities, en1, en2 ... en10.
					// we are here trying to create an arry of 2 list.  First list will be of values en1.c1, en2.c1, en3.c1 ... en10.c1 and second list will be en1.c2, en2.c2 ... en10.c2.
					for(int k = 0; k < primaryKeyCounts; k++)
					{
						primaryKeyValues[k].Add(keyValues[k]);
					}
				}
				foreach(var edge in node.AllChildEdges())
				{
					if (this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ChildReferenceOnParentNode)) continue;

					var additionalWhereClause = childWhereClauses?.Where(w => w.Item1 == edge).Select(w => w.Item2).ToArray();
					var additionalOrClauses = childOrClausesGroup?.Where(o => o.Item1 == edge).Select(o => o.Item2).ToList();

					Debug.Assert(edge.ChildNodeForeignKeys.Length >= primaryKeyCounts, "Number of foreign keys is less than that of parent's primary keys.");
					IDictionary<string, object[]> childKeys = new Dictionary<string, object[]>();
					int idx;
					for (idx = 0; idx < primaryKeyCounts; idx++)
					{
						childKeys.Add(edge.ChildNodeForeignKeys[idx].PropertyName, primaryKeyValues[idx].ToArray());

					}
					while (idx < edge.ChildNodeForeignKeys.Length)
					{
						Debug.Assert(edge.ChildNodeForeignKeys[idx].IsConstant == true, $"Found foreign key {edge.ChildNodeForeignKeys[idx].PropertyName} on {edge.ChildNode.Name} has no given value from parent and it's not a constant.");
						childKeys.Add(edge.ChildNodeForeignKeys[idx].PropertyName, null);
						idx++;
					}
					var foreignKeyReaders = new Func<object, dynamic>[primaryKeyCounts];
					Array.Copy(edge.ChildForeignKeyReaders, 0, foreignKeyReaders, 0, primaryKeyCounts);
					
					var connection = this.GetConnectionString(edge.ChildNode);
					// TODO: Select<Object> won't cut.  Need to fix object as the real type.
					var childrenGrpByPKeys = this.Driver.Select<object>(connection, edge.ChildNode, childKeys, additionalWhereClause, additionalOrClauses, innerJoinWhere, innerJoinOr, this.TimeoutDurationInSeconds);
					
					foreach(var pKeys in childrenGrpByPKeys.Keys)
					{
						int entityIdx = -1;
						if (entityIndexes.TryGetValue(pKeys, out entityIdx))
						{
							edge.ChildAddingAction(entities.ElementAt(entityIdx), childrenGrpByPKeys[pKeys]);
							foreach(var eachChild in childrenGrpByPKeys[pKeys])
							{
								edge.ParentSettingAction(eachChild, entities.ElementAt(entityIdx));
							}
						}
						else
						{
							throw new InvalidOperationException(
								string.Format(
								"Failed to locate parent object among list of entites by key values {0}",
								String.Join(", ", pKeys.Select(k => k?.ToString() ?? "null"))
								));
						}
					}

					Parallel.ForEach(
						childrenGrpByPKeys.Values,
						this.ParallelRetrievalOptions,
						(eachChild) =>
						{
							this.Build(eachChild, true, true, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
						});
				}
			}
			if (isMovingTowardsParent)
			{
				foreach (var entity in entities)
				{
					foreach (var eachChildToParent in node.AllParentEdgeNames())
					{
						var edge = node.GetEdgeToParent(eachChildToParent);
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
						this.Build(parent, true, false, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
					}
				}
			}
			return entities;
		}

		public T Build<T>(
			T entity, 
			bool isMovingTowardsParent, 
			bool isMovingTowardsChild, 
			IEnumerable<ValueTuple<NodeEdge, Expression>> childWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childOrClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			) where T : class, new()
		{
			Node node = this.NodeMap.GetNode(entity.GetType());

			if (isMovingTowardsChild)
			{
				foreach (var edge in node.AllChildEdges())
				{
					// child path excluded
					if (this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ChildReferenceOnParentNode)) continue;

					var additionalWhereClause = childWhereClauses?.Where(w => w.Item1 == edge).Select(w => w.Item2).ToArray();
					var additionalOrClauses = childOrClausesGroup?.Where(o => o.Item1 == edge).Select(o => o.Item2).ToList();

					var readers = edge.ParentPrimaryKeyReaders;
					IDictionary<string, object> childKeys = new Dictionary<string, object>();
					int i;
					for (i = 0; i < readers.Length && i < edge.ChildNodeForeignKeys.Length; i++)
					{
						childKeys.Add(edge.ChildNodeForeignKeys[i].PropertyName, readers[i].Invoke(entity));
					}
					while (i < edge.ChildNodeForeignKeys.Length)
					{
						Debug.Assert(edge.ChildNodeForeignKeys[i].IsConstant == true, $"Found foreign key {edge.ChildNodeForeignKeys[i].PropertyName} on {edge.ChildNode.Name} has no given value from parent and it's not a constant.");
						childKeys.Add(edge.ChildNodeForeignKeys[i].PropertyName, edge.ChildNodeForeignKeys[i].ConstantValue);
						i++;
					}
					var connection = this.GetConnectionString(edge.ChildNode);
					IEnumerable<object> children = null;
					// The result of the shorter select call can be cached as it pretty much fixed.  Thus don't want to mix these 2 calls.
					if (
						(additionalWhereClause != null && additionalWhereClause.Length > 0 ) ||
						(additionalOrClauses != null && additionalOrClauses.Count > 0) ||
						(innerJoinWhere != null && innerJoinWhere.Count() > 0) ||
						(innerJoinWhere != null && innerJoinWhere.Count() > 0)
						)
					{
						children = this.Driver.Select<object>(connection, edge.ChildNode, childKeys, additionalWhereClause, additionalOrClauses, innerJoinWhere, innerJoinOr, this.TimeoutDurationInSeconds);
					}
					else
					{
						children = this.Driver.Select<object>(connection, edge.ChildNode, childKeys, this.TimeoutDurationInSeconds);
					}
					edge.ChildAddingAction(entity, children);

					switch(this.Strategy)
					{
						case RetrievalStrategies.OneEntityAtATimeInParallel:
							Parallel.ForEach(
								children,
								this.ParallelRetrievalOptions,
								(eachChild) =>
								{
									edge.ParentSettingAction(eachChild, entity);
									this.Build(eachChild, true, true, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
								});
							break;
						case RetrievalStrategies.OneEntityAtATimeInSequence:
							foreach (var eachChild in children)
							{
								edge.ParentSettingAction(eachChild, entity);
								this.Build(eachChild, true, true, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
							}
							break;
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
					this.Build(parent, true, false, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
				}
			}
			return entity;
		}

		public int Save(ITrackable entity)
		{
			var rowCount = this.Save(entity, null, null);

			if (this.DeleteQueue.Count > 0)
			{
				var reverseOrderQueue = this.DeleteQueue.ToArray().Reverse();
				foreach(var eachDeleted in reverseOrderQueue)
				{
					var connectionString = this.GetConnectionString(eachDeleted.ChildNode);
					this.Driver.Delete<ITrackable>(connectionString, eachDeleted.ChildEntity, eachDeleted.ChildNode);
					// When the deleted is the root node, then there will be no edge.
					if (eachDeleted.Edge != null)
					{
						eachDeleted.Edge.ChildRemovingAction(eachDeleted.ParentEntity, eachDeleted.ChildEntity);
					}
				}
			}

			return rowCount;
		}

		private int Save(ITrackable entity, NodeEdge edge, ITrackable parent)
		{
			var node = this.NodeMap.GetNode(entity.GetType());
			Debug.Assert(node != null, $"Failed to locate node for entity of type '{entity.GetType().FullName}'");

			var connectionString = this.GetConnectionString(node);
			Debug.Assert(string.IsNullOrEmpty(connectionString) == false);

			var recordCount = 0;
			switch (entity.EntityStatus)
			{
				case EntityStatusEnum.New:
					this.Driver.Insert<ITrackable>(connectionString, entity, node);
					entity.EntityStatus = EntityStatusEnum.NoChange;
					recordCount++;
					break;
				case EntityStatusEnum.Updated:
					this.Driver.Update<ITrackable>(connectionString, entity, node);
					entity.EntityStatus = EntityStatusEnum.NoChange;
					recordCount++;
					break;
				case EntityStatusEnum.Deleted:
					recordCount++;
					//entity.EntityStatus = EntityStatusEnum.NoChange;
					this.DeleteQueue.Add((parent, edge, entity, node));
					break;
			}

			var childEdges = node.AllChildEdgeNames().Select(p => node.GetEdgeToChildren(p)).ToArray();
			foreach (var childEdge in childEdges)
			{
				// Skip child set if indicated in exclusion list.
				if (this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(childEdge.ChildReferenceOnParentNode)) continue;

				var iterator = childEdge.ChildReferenceOnParentNode.GetEnumeratorFunc(entity);

				while (iterator.MoveNext())
				{
					var child = iterator.Current;
					// if parent is new, children has to be new.  Same as delete.  If parent is deleted, so much be child.
					// TODO: Figure out how to stop Cascading Delete.
					if (entity.EntityStatus == EntityStatusEnum.New || entity.EntityStatus == EntityStatusEnum.Deleted)
					{
						child.EntityStatus = entity.EntityStatus;
					}
					if (child.EntityStatus == EntityStatusEnum.New)
					{
						this.PopulateChildForeignKeys(entity, node, childEdge);
					}
					recordCount += this.Save(child, childEdge, entity);
				}
			}
			return recordCount;
		}
		private void PopulateChildForeignKeys(ITrackable parent, Node node, NodeEdge edge)
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
		private string GetEdgeSignature(NodeEdge edge)
		{
			return $"{edge.ParentNode.FullName}.{edge.ChildReferenceOnParentNode?.PropertyName},{edge.ChildNode.FullName}.{edge.ParentReferenceOnChildNode?.PropertyName}";
		}
		private string GetConnectionString(Node node)
		{
			return string.IsNullOrEmpty(node.DbConnectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(node.DbConnectionName);
		}
	}
}
