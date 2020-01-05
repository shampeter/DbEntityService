using System;
using System.Reflection;
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
		private static MethodInfo buildAllChildrenInOneGoMethodInfo = typeof(Director).GetMethod(nameof(BuildAllChildrenInOneGo), BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo buildAllParentInOneGoMethodInfo = typeof(Director).GetMethod(nameof(BuildAllParentInOneGo), BindingFlags.NonPublic | BindingFlags.Instance);
		private IDatabaseDriver Driver { get; set; }
		private INodeMap NodeMap { get; set; }
		private IDictionary<Node, NodeProperty[]> Exclusion { get; set; }
		private ILogger Log { get; set; }
		private IDbServiceOption ServiceOption { get; set; }
		private int TimeoutDurationInSeconds { get; set; }
		private IList<(ITrackable ParentEntity, NodeEdge Edge, ITrackable ChildEntity, Node ChildNode)> DeleteQueue { get; set; }
		private ParallelOptions ParallelRetrievalOptions { get; set; }
		private RetrievalStrategies Strategy { get; set; }
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
		}
		
		public IEnumerable<T> Build<T>(
			ISet<NodeEdge> walkedPath,
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

			Node node = this.NodeMap.GetNode(entities.FirstOrDefault()?.GetType() ?? typeof(T));

			if (isMovingTowardsChild)
			{
				var entityIndexes = new Dictionary<object[], int>(new ObjectArrayComparer());
				var primaryKeyCounts = node.PrimaryKeys.Keys.Count;
				var primaryKeyValues = node.PrimaryKeys.Keys.Select(k => new List<object>()).ToArray();
				for (int i = 0; i < entities.Count(); i++)
				{
					// read primary key values for each entity into an object array.
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
					if (
						(this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ChildReferenceOnParentNode))
						||
						walkedPath.Contains(edge)
					)
					{
						continue;
					}
					else
					{
						walkedPath.Add(edge);
					}
					this.BuildByOneChildEdge<T>(walkedPath, entityIndexes, entities, node, primaryKeyCounts, primaryKeyValues, edge, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
				}
			}
			if (isMovingTowardsParent)
			{
				// TODO: Inner join failed in benchmark. Check it out.
				foreach (var edge in node.AllParentEdges())
				{
					if (
						(this.Exclusion.ContainsKey(node) && this.Exclusion[node].Contains(edge.ParentReferenceOnChildNode))
						|| 
						walkedPath.Contains(edge)
					)
					{
						continue;
					} 
					//else 
					//{
					//	walkedPath.Add(edge);
					//}
						

					this.BuildByOneParentEdge<T>(walkedPath, node, edge, entities, childWhereClauses, childOrClausesGroup, innerJoinWhere, innerJoinOr);
				}               
			}
			return entities;
		}
		private void BuildByOneParentEdge<T>(
			ISet<NodeEdge> walkedPath,
			Node childNode,
			NodeEdge childToParentEdge,
			IEnumerable<T> children,
			IEnumerable<ValueTuple<NodeEdge, Expression>> childWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> childOrClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			)
		{
			var parentKeys = childToParentEdge.ParentNodePrimaryKeys;
			var childFKeyReaders = childToParentEdge.ChildForeignKeyReaders.Take(parentKeys.Length);
			var childFKeyToEnumLoc = new Dictionary<object[], List<int>>(new ObjectArrayComparer());
			for (int idx = 0; idx < children.Count(); idx++)
			{
				var fKey = childFKeyReaders.Select(k => k.Invoke(children.ElementAt(idx))).ToArray();
				if (childFKeyToEnumLoc.TryGetValue(fKey, out var locList))
				{
					locList.Add(idx);
				}
				else
				{
					childFKeyToEnumLoc.Add(fKey, new List<int> { idx });
				}
			}
			var parentKeyParameters = new Dictionary<string, object[]>();
			var consolidatedKeys = childFKeyToEnumLoc.Keys.ToList();
			for (int kPos = 0; kPos < parentKeys.Length; kPos++)
			{
				parentKeyParameters.Add(parentKeys[kPos].PropertyName, consolidatedKeys.Select(v => v[kPos]).ToArray());
			}
			var actionType = typeof(Action<,,,,,,,,,>).MakeGenericType(
				typeof(ISet<NodeEdge>),
				typeof(IDictionary<object[], List<int>>),
				typeof(IEnumerable<T>),
				typeof(NodeEdge),
				typeof(Node),
				typeof(IDictionary<string, object[]>),
				typeof(IEnumerable<ValueTuple<NodeEdge, Expression>>),
				typeof(IEnumerable<ValueTuple<NodeEdge, Expression[]>>),
				typeof(IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)>),
				typeof(IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)>)
				);
			buildAllParentInOneGoMethodInfo
				.MakeGenericMethod(childToParentEdge.ChildNode.NodeType, childToParentEdge.ParentNode.NodeType)
				.CreateDelegate(actionType, this)
				.DynamicInvoke(
					walkedPath,
					childFKeyToEnumLoc,
					children,
					childToParentEdge,
					childToParentEdge.ParentNode,
					parentKeyParameters,
					childWhereClauses,
					childOrClausesGroup,
					innerJoinWhere,
					innerJoinOr
					);
			/*
					private void BuildAllParentInOneGo<TChild, TParent>(
						IDictionary<object[], List<int>> childFKeyToEnumLocs,
						IEnumerable<TChild> children,
						NodeEdge childToParentEdge,
						Node parentNode,
						IDictionary<string, object[]> parentKeys,
						IEnumerable<ValueTuple<NodeEdge, Expression>> fullChildWhereClauses,
						IEnumerable<ValueTuple<NodeEdge, Expression[]>> fullChildOrClausesGroup,
						IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
						IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
						)

			 */
		}
		private void BuildByOneChildEdge<T>(
			ISet<NodeEdge> walkedPath,
			Dictionary<object[], int> parentKeyToEnumLoc,
			IEnumerable<T> parents,
			Node parentNode,
			int primaryKeyCounts,
			List<object>[] primaryKeyValues,
			NodeEdge parentToChildEdge,
			IEnumerable<(NodeEdge, Expression)> childWhereClauses, 
			IEnumerable<(NodeEdge, Expression[])> childOrClausesGroup, 
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere, 
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			) where T : class, new()
		{
			var additionalWhereClause = childWhereClauses?.Where(w => w.Item1 == parentToChildEdge).Select(w => w.Item2).ToArray();
			var additionalOrClauses = childOrClausesGroup?.Where(o => o.Item1 == parentToChildEdge).Select(o => o.Item2).ToList();

			Debug.Assert(parentToChildEdge.ChildNodeForeignKeys.Length >= primaryKeyCounts, "Number of foreign keys is less than that of parent's primary keys.");
			var childKeys = new Dictionary<string, object[]>();
			int idx;
			for (idx = 0; idx < primaryKeyCounts; idx++)
			{
				childKeys.Add(parentToChildEdge.ChildNodeForeignKeys[idx].PropertyName, primaryKeyValues[idx].ToArray());

			}
			while (idx < parentToChildEdge.ChildNodeForeignKeys.Length)
			{
				Debug.Assert(parentToChildEdge.ChildNodeForeignKeys[idx].IsConstant == true, $"Found foreign key {parentToChildEdge.ChildNodeForeignKeys[idx].PropertyName} on {parentToChildEdge.ChildNode.Name} has no given value from parent and it's not a constant.");
				childKeys.Add(parentToChildEdge.ChildNodeForeignKeys[idx].PropertyName, null);
				idx++;
			}
			var additionalWhere = ExpressionHelper.RestoreWhereClause(parentToChildEdge.ChildNode, additionalWhereClause, out Type restoredWhereType);
			var additionalOr = ExpressionHelper.RestoreOrClauses(parentToChildEdge.ChildNode, additionalOrClauses, out Type restoredOrGrpType);
			var parentEnumType = typeof(IEnumerable<>).MakeGenericType(parentNode.NodeType);
			var actionType = typeof(Action<,,,,,,,,,,,>).MakeGenericType(
				typeof(ISet<NodeEdge>),
				typeof(IDictionary<object[], int>),
				parentEnumType,
				typeof(NodeEdge),
				typeof(Node),
				typeof(IDictionary<string, object[]>),
				restoredWhereType,
				restoredOrGrpType,
				typeof(IEnumerable<ValueTuple<NodeEdge, Expression>>),
				typeof(IEnumerable<ValueTuple<NodeEdge, Expression[]>>),
				typeof(IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)>),
				typeof(IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)>)
				);
			Director.buildAllChildrenInOneGoMethodInfo
				.MakeGenericMethod(parentNode.NodeType, parentToChildEdge.ChildNode.NodeType)
				.CreateDelegate(actionType, this)
				.DynamicInvoke(
					walkedPath,
					parentKeyToEnumLoc,
					parents,
					parentToChildEdge,
					parentToChildEdge.ChildNode,
					childKeys,
					additionalWhere,
					additionalOr,
					childWhereClauses,
					childOrClausesGroup,
					innerJoinWhere,
					innerJoinOr
				);
		}

		private void BuildAllChildrenInOneGo<TParent, TChild>(
			ISet<NodeEdge> walkedPath,
			IDictionary<object[], int> parentPKeyToEnumLocIdx,
			IEnumerable<TParent> parents,
			NodeEdge parentToChildEdge,
			Node childNode, 
			IDictionary<string, object[]> childKeys,
			IEnumerable<Expression<Func<TChild, bool>>> additionalWhereClause,
			IEnumerable<Expression<Func<TChild, bool>>[]> additionalOrClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression>> fullChildWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> fullChildOrClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			) where TChild : class, new()
		{
			var connection = this.GetConnectionString(childNode);
			var childrenGrpByPKeys = this.Driver.MultipleSelectCombined<TChild>(connection, childNode, childKeys, additionalWhereClause, additionalOrClauses, innerJoinWhere, innerJoinOr, this.TimeoutDurationInSeconds, this.ServiceOption.QueryBatchSize);

			foreach (var pKeys in childrenGrpByPKeys.Keys)
			{
				int entityIdx = -1;
				if (parentPKeyToEnumLocIdx.TryGetValue(pKeys, out entityIdx))
				{
					parentToChildEdge.ChildAddingAction(parents.ElementAt(entityIdx), childrenGrpByPKeys[pKeys]);
					foreach (var eachChild in childrenGrpByPKeys[pKeys])
					{
						parentToChildEdge.ParentSettingAction(eachChild, parents.ElementAt(entityIdx));
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

			this.Build<TChild>(walkedPath, childrenGrpByPKeys.Values.SelectMany(p => p).ToList(), true, true, fullChildWhereClauses, fullChildOrClausesGroup, innerJoinWhere, innerJoinOr);
		}

		private void BuildAllParentInOneGo<TChild, TParent>(
			ISet<NodeEdge> walkedPath,
			IDictionary<object[], List<int>> childFKeyToEnumLocs,
			IEnumerable<TChild> children,
			NodeEdge childToParentEdge,
			Node parentNode,
			IDictionary<string, object[]> parentKeys,
			IEnumerable<ValueTuple<NodeEdge, Expression>> fullChildWhereClauses,
			IEnumerable<ValueTuple<NodeEdge, Expression[]>> fullChildOrClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> expressions)> innerJoinWhere,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> expressions)> innerJoinOr
			)
			where TParent : class, new()
		{
			var connection = this.GetConnectionString(parentNode);
			var parentGrpByFKeys = this.Driver.MultipleSelectCombined<TParent>(
										connection,
										parentNode,
										parentKeys,
										Array.Empty<Expression<Func<TParent, bool>>>(),
										Array.Empty<Expression<Func<TParent, bool>>[]>(),
										Array.Empty<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)>(),
										Array.Empty<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)>(),
										this.TimeoutDurationInSeconds,
										this.ServiceOption.QueryBatchSize
										);
			foreach (var fKeys in parentGrpByFKeys.Keys)
			{
				foreach(var parent in parentGrpByFKeys[fKeys])
				{
					if (childFKeyToEnumLocs.TryGetValue(fKeys, out var entityIdxs))
					{
						var childrenList = new List<TChild>();
						foreach (var entityIdx in entityIdxs)
						{
							childToParentEdge.ParentSettingAction(children.ElementAt(entityIdx), parent);
							childrenList.Add(children.ElementAt(entityIdx));
						}
						childToParentEdge.ChildAddingAction(parent, (IEnumerable<object>)childrenList);
					}
					else
					{
						throw new InvalidOperationException(
							string.Format(
							"Failed to locate child object among list of entites by key values {0}",
							String.Join(", ", fKeys.Select(k => k?.ToString() ?? "null"))
							));
					}
				}
			}

			this.Build<TParent>(walkedPath, parentGrpByFKeys.Values.SelectMany(p => p).ToList(), true, false, fullChildWhereClauses, fullChildOrClausesGroup, innerJoinWhere, innerJoinOr);
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
