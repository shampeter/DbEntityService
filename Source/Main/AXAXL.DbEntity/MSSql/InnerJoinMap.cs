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
		private IList<ValueTuple<int, int, NodeEdge>> innerJoins;

		public InnerJoinMap()
		{
			this.edgeKeyIndexes = new Dictionary<string, int>();
			this.innerJoins = new List<ValueTuple<int, int, NodeEdge>>();
		}

		public IEnumerable<(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge)> Joins => this.innerJoins;

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
					this.innerJoins.Add((currentAliasIdx, this.runningSequence, edge));
				}
				else
				{
					edge = node.GetEdgeToParent(newJoinEnittyRef);
					this.innerJoins.Add((this.runningSequence, currentAliasIdx, edge));
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
			(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge) topLevelJoin = (queryRootAliaxIdx, queryRootAliaxIdx, null);
			var rootKey = this.FormatEdgeKey(queryRootNode, null, true);

			this.runningSequence = queryRootAliaxIdx;
			this.edgeKeyIndexes.Add(rootKey, this.runningSequence++);
			this.innerJoins.Add(topLevelJoin);

			return rootKey;
		}

		private string FormatEdgeKey(Node node, String entityRef, bool isRoot = false)
		{
			if (isRoot) return $"{node.Name}.-";

			Debug.Assert(node.IsPropertyOnNode(entityRef) && (node.ContainsEdgeToChildren(entityRef) || node.ContainsEdgeToParent(entityRef)));
			return $"{node.Name}.{entityRef}";
		}
	}
}
