using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.MSSql
{
	internal class InnerJoinMap : IInnerJoinMap
	{
		private int runningSequence;
		private IDictionary<string, int> edgeKeyIndexes;
		private IList<ValueTuple<int, int, NodeEdge, bool>> innerJoins;
		private string rootMapKey;
		public InnerJoinMap()
		{
			this.edgeKeyIndexes = new Dictionary<string, int>();
			this.innerJoins = new List<ValueTuple<int, int, NodeEdge, bool>>();
		}

		public IEnumerable<(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge, bool isTowardParent)> Joins => this.innerJoins;

		public string RootMapKey => this.rootMapKey;

		public string Add(string currentJoinKey, Node node, string newJoinEnittyRef)
		{
			var key = this.FormatEdgeKey(node, newJoinEnittyRef);
			if (! this.edgeKeyIndexes.ContainsKey(key))
			{
				var currentAliasIdx = this.GetAliasIndex(currentJoinKey);
				NodeEdge edge;
				if (node.ContainsEdgeToChildren(newJoinEnittyRef))
				{
					edge = node.GetEdgeToChildren(newJoinEnittyRef);
					this.innerJoins.Add((currentAliasIdx, this.runningSequence, edge, false));
				}
				else
				{
					edge = node.GetEdgeToParent(newJoinEnittyRef);
					this.innerJoins.Add((this.runningSequence, currentAliasIdx, edge, true));
				}
				this.edgeKeyIndexes.Add(key, this.runningSequence++);
			}
			return key;
		}

		public int GetAliasIndex(string mapKey)
		{
			Debug.Assert(edgeKeyIndexes.ContainsKey(mapKey));
			return this.edgeKeyIndexes[mapKey];
		}

		public string Init(Node queryRootNode, int queryRootAliaxIdx)
		{
			//(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) topLevelJoin = (queryRootAliaxIdx, queryRootAliaxIdx, null);
			this.rootMapKey = this.FormatEdgeKey(queryRootNode, null, true);
			this.edgeKeyIndexes.Add(this.rootMapKey, queryRootAliaxIdx);
			this.runningSequence = queryRootAliaxIdx + 1;
			//this.innerJoins.Add(topLevelJoin);
			return this.rootMapKey;
		}

		private string FormatEdgeKey(Node node, String entityRef, bool isRoot = false)
		{
			if (isRoot) return $"{node.Name}.-";

			Debug.Assert(node.IsPropertyOnNode(entityRef) && (node.ContainsEdgeToChildren(entityRef) || node.ContainsEdgeToParent(entityRef)));
			return $"{node.Name}.{entityRef}";
		}
	}
}
