using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Extensions;

namespace AXAXL.DbEntity.EntityGraph
{
	public class NodeMap : INodeMap
	{
		private IDictionary<Type, Node> _nodeMap = new ConcurrentDictionary<Type, Node>();
		private ILogger log = null;
		private IDatabaseDriver driver = null;
		public NodeMap(ILoggerFactory factory, IDatabaseDriver driver)
		{
			this.log = factory.CreateLogger<NodeMap>();
			this.driver = driver;
			Debug.Assert(this.log != null);
			Debug.Assert(this.driver != null);
		}
		public void BuildNodes(Assembly[] assemblies, string[] assemblyNamePrefixes, string filenameToDebugPrintMap = null)
		{
			var saveDebugInfo = string.IsNullOrEmpty(filenameToDebugPrintMap) == false;

			if (assemblies == null || assemblies.Length <= 0)
			{
				assemblies = AppDomain.CurrentDomain.GetAssemblies();
			}
			foreach(var assembly in assemblies)
			{
				//Parallel.ForEach(
				//	assembly.GetTypes(), 
				//	(type) => {
				//		var node = this.BuildNode(type);
				//		if (node != null)
				//		{
				//			_nodeMap.Add(type, node);
				//		}
				//	}
				//);
				if (
					assemblyNamePrefixes == null || 
					assemblyNamePrefixes.Length <= 0 || 
					assemblyNamePrefixes.Any(p => assembly.FullName.StartsWith(p, StringComparison.OrdinalIgnoreCase))
				)
				{
					foreach (var type in assembly.GetTypes())
					{
						var node = this.BuildNode(type, saveDebugInfo);
						if (node != null)
						{
							_nodeMap.Add(type, node);
						}
					}
				}
			}
			this.BuildNodeEdges(saveDebugInfo);
			this.PrintInMarkDown(filenameToDebugPrintMap);
		}
		public bool ContainsNode(Type type)
		{
			return this._nodeMap.ContainsKey(type);
		}
		public Node GetNode(Type type)
		{
			return this.ContainsNode(type) ? this._nodeMap[type] : null;
		}
		[Conditional("DEBUG")]
		public void PrintInMarkDown(string filenameToDebugPrintMap)
		{
			if (string.IsNullOrEmpty(filenameToDebugPrintMap)) return;

			using (var writer = new StreamWriter(filenameToDebugPrintMap))
			{
				writer
					.PrintLine(@"# ALL NODES")
					.PrintLine();
				foreach(var p in this._nodeMap.Values)
				{
					p.PrintMarkDown(writer);
				}
			}

			this.log.LogDebug(@"Written full node map to {0}", filenameToDebugPrintMap);
		}
		public Node BuildNode(Type type, bool saveExpressionToStringForDebug = false)
		{
			if (type.IsEntity() == false) return null;

			var primaryKeys = new List<NodeProperty>();
			var concurrency = new List<NodeProperty>();
			var dataColumns = new List<NodeProperty>();

			var newNode = new Node(type, this.log);

			foreach(var property in type.GetProperties())
			{
				var newProp = new NodeProperty(this.log)
				{
					Owner            = newNode,
					PropertyName     = property.Name,
					PropertyType     = property.PropertyType,
					PropertyCategory = property.GetPropertyTypeClassification(),
					IsNullable       = property.IsPropertyANullable()
				};
				newProp
					.HandleColumnAttribute(property, driver)
					.HandleDataGenerationAttribute(property)
					.HandleValueInjectionAttribute(property)
					.HandleActionInjectionAttribute(property)
					.HandleConstantAttribute(property)
					.HandleForeignKeyAttribute(property)
					.HandleInversePropertyAttribute(property)
					.CompileScript()
					.CompileDelegateForHandlingCollection(saveExpressionToStringForDebug)
					;
				
				if (property.IsPrimaryKey())
				{
					primaryKeys.Add(newProp);
				}
				else if (property.IsConcurrencyCheck())
				{
					concurrency.Add(newProp);
				}
				else
				{
					dataColumns.Add(newProp);
				}
			}
			newNode.PrimaryKeys = primaryKeys.ToDictionary(k => k.PropertyName, v => v);
			newNode.ConcurrencyControl = concurrency.FirstOrDefault();
			newNode.DataColumns = dataColumns.ToDictionary(k => k.PropertyName, v => v);
			newNode
				.HandleTableAttribute()
				.HandleConnectionAttribute()
				.LocateEdges();

			return newNode;
		}
		protected void BuildNodeEdges(bool saveExpressionToStringForDebug = false)
		{
			foreach(var node in this._nodeMap.Values)
			{
				foreach(var edgeProperty in node.DataColumns.Values.Where(c => c.IsEdge).ToArray())
				{
					this.HandleForeignKeyFoundOnParent(node, edgeProperty);
					this.HandleForeignKeyFoundOnChild(node, edgeProperty);
					// this.HandleInversePropertyFoundOnParent(node, edgeProperty);
					// this.HandleInversePropertyFoundOnChild(node,edgeProperty);
				}
				foreach (var edgeProperty in node.DataColumns.Values.Where(c => c.IsEdge).ToArray())
				{
					// this.HandleForeignKeyFoundOnParent(node, edgeProperty);
					// this.HandleForeignKeyFoundOnChild(node, edgeProperty);
					this.HandleInversePropertyFoundOnParent(node, edgeProperty);
					this.HandleInversePropertyFoundOnChild(node, edgeProperty);
				}
			}
			HashSet<NodeEdge> allUniqueEdges = new HashSet<NodeEdge>();
			foreach (var node in this._nodeMap.Values)
			{
				foreach(var eachEdge in node.AllChildEdgeNames().Select(p => node.GetEdgeToChildren(p)))
				{
					allUniqueEdges.Add(eachEdge);
				}
				foreach(var eachEdge in node.AllParentEdgeNames().Select(p => node.GetEdgeToParent(p)))
				{
					allUniqueEdges.Add(eachEdge);
				}
			}
			
			foreach(var eachEdge in allUniqueEdges)
			{
				eachEdge
					.SortKeysByOrder()
					.CompileChildAddingAction(saveExpressionToStringForDebug)
					.CompileChildRemoveAction(saveExpressionToStringForDebug)
					.CompileParentSettingAction(saveExpressionToStringForDebug)
					.CompileParentPrimaryKeyReaders(saveExpressionToStringForDebug)
					.CompileChildForeignKeyReaders(saveExpressionToStringForDebug)
					.CompileChildForeignKeyWriters(saveExpressionToStringForDebug)
					;
			}
		}
		protected void HandleForeignKeyFoundOnParent(Node argNode, NodeProperty argEdgeProperty)
		{
			var foreignKeyReference = argEdgeProperty.ForeignKeyReference;
			// No foreign key defined. Skip.
			if (foreignKeyReference == null || argEdgeProperty.PropertyCategory != PropertyCategories.Collection) return;

			Debug.Assert(
				argNode.ContainsEdgeToChildren(argEdgeProperty), 
				$"Bootstrap Error!  Should have found an edge on property '{argEdgeProperty.PropertyName}' of '{argNode.NodeType.Name}' pointing to children");
			var edge = argNode.GetEdgeToChildren(argEdgeProperty);

			var childNode = this.GetNodeReferencedByEdge(argEdgeProperty);
			var fKeyPropertyOnChild = childNode.GetPropertiesFromNode(foreignKeyReference);

			edge.ChildNode = childNode;
			edge.ChildNodeForeignKeys = fKeyPropertyOnChild;
		}
		protected void HandleForeignKeyFoundOnChild(Node node, NodeProperty edgeProperty)
		{
			var foreignKeyReference = edgeProperty.ForeignKeyReference;
			// No foreign key defined. Skip.
			if (foreignKeyReference == null || edgeProperty.PropertyCategory == PropertyCategories.Collection) return;

			var edge = node.GetEdgeToParent(edgeProperty);

			var parentNode = this.GetNodeReferencedByEdge(edgeProperty);
			var pKeyPropertyOnParent = parentNode.PrimaryKeys.Values.ToArray();
			edge.ParentNode = parentNode;
			edge.ParentNodePrimaryKeys = pKeyPropertyOnParent;
		}
		protected void HandleInversePropertyFoundOnParent(Node node, NodeProperty edgeProperty)
		{
			var inversePropertyReference = edgeProperty.InversePropertyReference;
			// No inverse property defined. Skip.
			if (string.IsNullOrEmpty(inversePropertyReference) || edgeProperty.PropertyCategory != PropertyCategories.Collection) return;

			var childNode = this.GetNodeReferencedByEdge(edgeProperty);
			var parentRefPropOnChild = childNode.GetPropertyFromNode(inversePropertyReference);
			Debug.Assert(parentRefPropOnChild.PropertyCategory == PropertyCategories.ObjectReference, $"Parent reference '{inversePropertyReference}' on child {childNode.NodeType.Name} is not of object reference type.");
			Debug.Assert(node.ContainsEdgeToChildren(edgeProperty), $"Bootstrap failure.  Edge on '{edgeProperty.PropertyName}' should have been added already on '{node.NodeType.Name}'.");
			var edgeToChildren = node.GetEdgeToChildren(edgeProperty);
			edgeToChildren.ParentReferenceOnChildNode = parentRefPropOnChild;
			if (childNode.ContainsEdgeToParent(parentRefPropOnChild) == false)
			{
				childNode.AddEdgeOnChildToParent(parentRefPropOnChild, edgeToChildren);
			}
			else
			{
				var edgeToParent = childNode.GetEdgeToParent(parentRefPropOnChild);
				if (edgeToChildren != edgeToParent)
				{
					edgeToChildren.Merge(edgeToParent);
					childNode.AddEdgeOnChildToParent(parentRefPropOnChild, edgeToChildren, true, false);
				}
			}
		}
		protected void HandleInversePropertyFoundOnChild(Node argNode, NodeProperty argEdgeProperty)
		{
			if (argEdgeProperty.PropertyCategory != PropertyCategories.ObjectReference) return;
			var inversePropertyReference = argEdgeProperty.InversePropertyReference;
			// No inverse property defined. Skip.
			if (string.IsNullOrEmpty(inversePropertyReference) || argEdgeProperty.PropertyCategory != PropertyCategories.ObjectReference) return;

			var parentNode = this.GetNodeReferencedByEdge(argEdgeProperty);
			var childRefPropOnParent = parentNode.GetPropertyFromNode(inversePropertyReference);
			Debug.Assert(argNode.ContainsEdgeToParent(argEdgeProperty), $"Program failure.  Edge on '{argEdgeProperty.PropertyName}' should have been added already.");
			var edgeToParent = argNode.GetEdgeToParent(argEdgeProperty);
			edgeToParent.ParentNode = parentNode;
			edgeToParent.ParentNodePrimaryKeys = parentNode.PrimaryKeys.Values.ToArray();
			edgeToParent.ChildReferenceOnParentNode = childRefPropOnParent;
			if (parentNode.ContainsEdgeToChildren(childRefPropOnParent) == false)
			{
				parentNode.AddEdgeOnParentToChild(childRefPropOnParent, edgeToParent);
			}
			else
			{
				var edgeToChild = parentNode.GetEdgeToChildren(childRefPropOnParent);
				if (edgeToChild != edgeToParent)
				{
					edgeToChild.Merge(edgeToParent);
					parentNode.AddEdgeOnParentToChild(childRefPropOnParent, edgeToChild, true, false);
				}
			}
		}
		// protected NodeBuilder BuildParentToChildEdge(IDictionary<Type, Node> argNodes, Node argNode, NodeProperty argEdgeProperty)
		// {
		// 	if (argEdgeProperty.PropertyCategory != PropertyCategories.Collection) return this;

		// 	var childSetPropName = argEdgeProperty.PropertyName;
		// 	var nodeTypeName = argNode.NodeType.Name;
		// 	var fKey = argEdgeProperty.PropertyType.GetCustomAttribute<ForeignKeyAttribute>();
		// 	var fKeyPropName = fKey != null ? fKey.Name : string.Empty;
		// 	var InvRef = argEdgeProperty.PropertyType.GetCustomAttribute<InversePropertyAttribute>();
		// 	var InvRefPropName = InvRef != null ? InvRef.Property : string.Empty;

		// 	// to make sure that ForeignKeyAttribute ahs a name.
		// 	Debug.Assert(fKey == null || ! string.IsNullOrEmpty(fKeyPropName), $"Name in ForeignKeyAttribute on {childSetPropName} of {nodeTypeName} is missing!");
		// 	// to make sure that inverse reference has a name
		// 	Debug.Assert(InvRef == null || ! string.IsNullOrEmpty(InvRefPropName), $"Name in InversePropertyAttribute on {childSetPropName} of {nodeTypeName} is missing!");
		// 	// to make sure that at least one of Foreign Key or Inverse Property is there.
		// 	Debug.Assert(!string.IsNullOrEmpty(fKeyPropName) || !string.IsNullOrEmpty(InvRefPropName), $"There should be either foreign key or inverse property or both but not none on {childSetPropName} of {nodeTypeName}.");
		// 	// for child set, only work with generic class otherwise, can't figure out the child class.
		// 	Debug.Assert(argEdgeProperty.PropertyType.IsGenericType, $"{childSetPropName} on {nodeTypeName} is not a generic collection.");
		// 	// to make sure the collection has only 1 generic parameter.
		// 	Debug.Assert(argEdgeProperty.PropertyType.GetGenericArguments().Length == 1 , $"{childSetPropName} on {nodeTypeName} is a generic collection with more than 1 generic parameter!");

		// 	var childType = argEdgeProperty.PropertyType.GetGenericArguments().Single();
		// 	Debug.Assert(argNodes.ContainsKey(childType), $"Childset type {childType.Name} is not found");
		// 	var childNode = argNodes[childType];
		// 	NodeProperty foreignKeyPropOnChild = null;
		// 	NodeProperty parentRefPropOnChild = null;
		// 	childNode.DataColumns.TryGetValue(InvRefPropName, out parentRefPropOnChild);
		// 	if (childNode.DataColumns.TryGetValue(fKeyPropName, out foreignKeyPropOnChild) == false)
		// 	{
		// 		foreignKeyPropOnChild = childNode.FindForeignKeyForParentReferenceOnChild(InvRefPropName);
		// 	}
		// 	var parentToChildrenEdge = new NodeEdge{
		// 		ParentNode = argNode,
		// 		ParentNodePrimaryKeys = argNode.PrimaryKeys.Values.ToArray(),
		// 		ChildReferenceOnParentNode = argEdgeProperty,
		// 		ChildNode = childNode,
		// 		ChildNodeForeignKeys = new [] {foreignKeyPropOnChild},
		// 		ParentReferenceOnChildNode = parentRefPropOnChild
		// 	};
		// 	argNode.EdgeToChildren.Add(foreignKeyPropOnChild.PropertyName, parentToChildrenEdge);
		// 	return this;
		// }
		// protected NodeBuilder BuildChildToParentEdge(IDictionary<Type, Node> argNodes, Node argNode, NodeProperty argEdgeProperty)
		// {
		// 	if (argEdgeProperty.PropertyCategory == PropertyCategories.Collection) return this;

		// 	var foreignPropOnChild = argNode.FindForeignKeyForParentReferenceOnChild(argEdgeProperty.PropertyName);

		// 	return this;
		// }
		protected Node GetNodeReferencedByEdge(NodeProperty property)
		{
			var typeReferencedByEdge = property.GetTypeReferencedByEdge();
			Debug.Assert(
				this._nodeMap.ContainsKey(typeReferencedByEdge), 
				$"Type '{typeReferencedByEdge.Name}' referenced by property '{property.PropertyName}' on '{property.Owner.NodeType.Name}' cannot found");

			return this._nodeMap[typeReferencedByEdge];
		}
	}
}