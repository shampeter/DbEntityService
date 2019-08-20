using System;
using System.Reflection;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	public interface INodeMap
	{
		void BuildNodes(params Assembly[] assemblies);
		bool ContainsNode(Type type);
		Node GetNode(Type type);
	}
}