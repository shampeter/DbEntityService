using System;
using System.Reflection;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// NodeMap contains all meta data with respect to object-relational mapping and parent-to-child relationship found among loaded or specified assemblies.
	/// Meta data of an entity class, identifiable by <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/>, is stored as a Node.
	/// Meta data of an entity class property, identifiable by <see cref="System.ComponentModel.DataAnnotations.Schema.ColumnAttribute"/> is stored as a NodeProperty.
	/// Meta data of entity class relationship, such as references and primary-to-foreign key mapping, is stored as NodeEdge.
	/// </summary>
	public interface INodeMap
	{
		/// <summary>
		/// Build up meta data on entity objects found in <paramref name="assemblies"/> or all loaded assemblies as found in <see cref="AppDomain.CurrentDomain"/>
		/// if <paramref name="assemblies"/> is null or empty.
		/// Use <paramref name="assemblyNamePrefixes"/> to narrow down the assembly to start by specifying the assembly name prefix.
		/// Service will use case insensitives match to test assembly by name with <see cref="string.StartsWith(string, StringComparison)"/>
		/// If <paramref name="assemblyNamePrefixes"/> is null or empty, service will search through all assemblies specified.
		/// </summary>
		/// <param name="assemblies">Optional.  List of assemblies to search for meta data on entity class.</param>
		/// <param name="assemblyNamePrefixes">Name prefixes.</param>
		/// <param nane="filenameToDebugPrintMap">when set, print the full map built to a markdown file of this given location.</param>
		void BuildNodes(Assembly[] assemblies, string[] assemblyNamePrefixes, string filenameToDebugPrintMap = null);
		/// <summary>
		/// True if a <see cref="Node"/> is found for the parameter <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Type of entity class to lookup</param>
		/// <returns>True if <see cref="Node"/> is found.</returns>
		bool ContainsNode(Type type);
		/// <summary>
		/// Return a <see cref="Node"/> of the requested <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Entity class type</param>
		/// <returns>A <see cref="Node"/> that represent the meta data on this entity class <paramref name="type"/></returns>
		Node GetNode(Type type);
	}
}