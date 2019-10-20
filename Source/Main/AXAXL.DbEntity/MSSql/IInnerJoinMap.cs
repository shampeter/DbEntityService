using System;
using System.Collections.Generic;
using System.Text;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.MSSql
{
	internal interface IInnerJoinMap
	{
		/// <summary>
		/// Initialize the map with root node of the query and its corresponding starting sequence number.
		/// For example, if <paramref name="queryRootAliaxIdx"/> is 0, rest of table alias index will be 1, 2, 3 so on and so forth.
		/// </summary>
		/// <param name="queryRootNode">root node of a query.</param>
		/// <param name="queryRootAliaxIdx">starting index of table alias sequence</param>
		/// <returns>key to this map that can be used to retrieve the assigned table alias index.</returns>
		string Init(Node queryRootNode, int queryRootAliaxIdx);
		/// <summary>
		/// Add the edge to the inner join map as specified by <paramref name="node"/> and <paramref name="entityRef"/>
		/// </summary>
		/// <param name="currentJoinKey">key to left hand side of the join table.</param>
		/// <param name="node">node and <paramref name="entityRef"/> identify the edge for a join</param>
		/// <param name="entityRef"><paramref name="node"/> and entityRef identify the edge for a join</param>
		/// <returns>key to map that can be used to retrieve the assigned table alias index.</returns>
		string Add(string currentJoinKey, Node node, string entityRef);
		/// <summary>
		/// Return the table alias assigned to an entity among the joins.
		/// </summary>
		/// <param name="mapKey">String.  Map key.</param>
		/// <returns>Int.  Table alias index.</returns>
		int GetAliasIndex(string mapKey);
		IEnumerable<(int ParentTableAliasIdx, int ChildTableAliasIdx, NodeEdge Edge)> Joins { get;  }
	}
}
