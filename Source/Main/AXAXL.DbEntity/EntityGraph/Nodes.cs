using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Extensions;

namespace AXAXL.DbEntity.EntityGraph
{
	public class Node
	{
		private ILogger Log { get; set; }
		private string _name;
		private string _fullName;
		private NodeProperty[] _allDbColumns;
		public Node(Type nodeType, ILogger log)
		{
			this.PrimaryKeys = new Dictionary<string, NodeProperty>();
			this.DataColumns = new Dictionary<string, NodeProperty>();
			this.EdgeToChildren = new Dictionary<string, NodeEdge>();
			this.EdgeToParent = new Dictionary<string, NodeEdge>();
			this.Log = log;
			this.NodeType = nodeType;
		}
		public string Name
		{
			get
			{
				if (this._name == null)
				{
					this._name = this.NodeType.Name;
				}
				return this._name;
			}
		}
		public string FullName
		{
			get
			{
				if (this._fullName == null)
				{
					this._fullName = this.NodeType.FullName;
				}
				return this._fullName;
			}
		}
		public Type NodeType { get; set; }
		public string DbTableName { get; set; }
		public string DbSchemaName { get; set; }
		public String DbConnectionName { get; set; }
		public IDictionary<string, NodeProperty> PrimaryKeys { get; set; }
		public Func<object, dynamic>[] PrimaryKeyReaders { get; set; }
		public IDictionary<string, NodeProperty> DataColumns { get; set; }
		public NodeProperty ConcurrencyControl { get; set; }
		protected IDictionary<string, NodeEdge> EdgeToChildren { get; set; }
		protected IDictionary<string, NodeEdge> EdgeToParent { get; set; }
		public bool IsPropertyOnNode(string propertyName)
		{
			return
				this.PrimaryKeys.ContainsKey(propertyName) ||
				this.DataColumns.ContainsKey(propertyName) ||
				(this.ConcurrencyControl != null && this.ConcurrencyControl.PropertyName.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
				;
		}
		public bool IsPropertyOnNode(string[] propertyNames)
		{
			return propertyNames.All(p => this.IsPropertyOnNode(p) == true);
		}
		public NodeProperty GetPropertyFromNode(string propertyName)
		{
			Debug.Assert(this.IsPropertyOnNode(propertyName) == true, $"Cannot find property named '{propertyName}' on {this.Name}");

			NodeProperty property = null;
			bool found =
				this.PrimaryKeys.TryGetValue(propertyName, out property) ||
				this.DataColumns.TryGetValue(propertyName, out property);
			return found ? property : this.ConcurrencyControl;
		}
		public NodeProperty[] GetPropertiesFromNode(string[] propertyNames)
		{
			Debug.Assert(this.IsPropertyOnNode(propertyNames) == true, $"Cannot find properties {string.Join(", ", propertyNames)} on {this.Name}");
			return propertyNames.Select(p => this.GetPropertyFromNode(p)).ToArray();
		}
		public bool ContainsEdgeToChildren(NodeProperty property)
		{
			return this.ContainsEdgeToChildren(property.PropertyName);
		}
		public bool ContainsEdgeToChildren(string propertyName)
		{
			return this.EdgeToChildren.ContainsKey(propertyName);
		}
		public bool ContainsEdgeToParent(NodeProperty property)
		{
			return this.ContainsEdgeToParent(property.PropertyName);
		}
		public bool ContainsEdgeToParent(string propertyName)
		{
			return this.EdgeToParent.ContainsKey(propertyName);
		}
		public NodeEdge GetEdgeToChildren(NodeProperty property)
		{
			return this.GetEdgeToChildren(property.PropertyName);
		}
		public NodeEdge GetEdgeToChildren(string propertyName)
		{
			return this.EdgeToChildren[propertyName];
		}
		public NodeEdge GetEdgeToParent(NodeProperty property)
		{
			return this.GetEdgeToParent(property.PropertyName);
		}
		public NodeEdge GetEdgeToParent(string propertyName)
		{
			return this.EdgeToParent[propertyName];
		}
		public void AddEdgeOnParentToChild(NodeProperty property, NodeEdge edge, bool overwrite = true, bool throwExceptionIfAlreadyExisted = false)
		{
			this.AddEdgeOnParentToChild(property.PropertyName, edge, overwrite, throwExceptionIfAlreadyExisted);
		}
		public void AddEdgeOnParentToChild(string property, NodeEdge edge, bool overwrite = true, bool throwExceptionIfAlreadyExisted = false)
		{
			var existed = this.ContainsEdgeToChildren(property);
			if (existed)
			{
				if (!overwrite && throwExceptionIfAlreadyExisted)
				{
					throw new ArgumentException($"Edge to child on {property} already exists");
				}
			}
			if (! existed || overwrite)
			{
				this.EdgeToChildren[property] = edge;
			}
		}
		public void AddEdgeOnChildToParent(NodeProperty property, NodeEdge edge, bool overwrite = true, bool throwExceptionIfAlreadyExisted = false)
		{
			this.AddEdgeOnChildToParent(property.PropertyName, edge, overwrite, throwExceptionIfAlreadyExisted);
		}
		public void AddEdgeOnChildToParent(string property, NodeEdge edge, bool overwrite = true, bool throwExceptionIfAlreadyExisted = false)
		{
			var existed = this.ContainsEdgeToParent(property);
			if (existed)
			{
				if (!overwrite && throwExceptionIfAlreadyExisted)
				{
					throw new ArgumentException($"Edge to parent on {property} already exists");
				}
			}
			if (!existed || overwrite)
			{
				this.EdgeToParent[property] = edge;
			}
		}
		internal Node LocateEdges()
		{
			// case 1: InversePropertyAttribute found on property of type object or collection.
			// case 2: ForeignKeyAttribute found on property of type object or collection.
			// case 3: ForeignKeyAttribute found on value-typed property and the Name property of ForeignKeyAttribute identify the edge.
			foreach (var eachColumn in this.DataColumns.Values)
			{
				var foreignKeyRef = eachColumn.ForeignKeyReference;
				var inversePropRef = eachColumn.InversePropertyReference;
				var edgeColumn = eachColumn;
				var foreignKeyColumn = new[] { eachColumn };
				var edge = new NodeEdge(this.Log);
				if (foreignKeyRef != null)
				{
					switch (eachColumn.PropertyCategory)
					{
						case PropertyCategories.Value:
							edgeColumn = this.GetPropertyFromNode(foreignKeyRef[0]);
							break;
						case PropertyCategories.ObjectReference:
							foreignKeyColumn = this.GetPropertiesFromNode(foreignKeyRef);
							break;
						case PropertyCategories.Collection:
							foreignKeyColumn = null;
							break;
					}
					edgeColumn.IsEdge = true;

					if (edgeColumn.PropertyCategory == PropertyCategories.Collection)
					{
						if (this.ContainsEdgeToChildren(eachColumn) == false)
						{
							edge.ParentNode = this;
							edge.ParentNodePrimaryKeys = this.PrimaryKeys.Values.ToArray();
							edge.ChildReferenceOnParentNode = edgeColumn;
							this.EdgeToChildren.Add(edgeColumn.PropertyName, edge);
						}
					}
					else
					{
						if (this.ContainsEdgeToParent(edgeColumn) == false)
						{
							edge.ChildNode = this;
							edge.ParentReferenceOnChildNode = edgeColumn;
							edge.ChildNodeForeignKeys = foreignKeyColumn;
							this.EdgeToParent.Add(edgeColumn.PropertyName, edge);
						}
						else
						{
							edge = this.GetEdgeToParent(edgeColumn);
							if (edge.ChildNodeForeignKeys.Contains(foreignKeyColumn[0]) == false)
							{
								edge.ChildNodeForeignKeys = edge.ChildNodeForeignKeys.Append(foreignKeyColumn[0]).ToArray();
							}
						}
					}
				}
				if (!string.IsNullOrEmpty(inversePropRef))
				{
					Debug.Assert(eachColumn.PropertyCategory != PropertyCategories.Value, $"InversePropertyAttribute cannot apply to property '{eachColumn.PropertyName}' which is of value type.");
					eachColumn.IsEdge = true;
					if (eachColumn.PropertyCategory == PropertyCategories.Collection)
					{
						if (this.ContainsEdgeToChildren(eachColumn) == false)
						{
							edge.ParentNode = this;
							edge.ParentNodePrimaryKeys = this.PrimaryKeys.Values.ToArray();
							edge.ChildReferenceOnParentNode = edgeColumn;
							this.EdgeToChildren.Add(edgeColumn.PropertyName, edge);
						}
					}
					else if (eachColumn.PropertyCategory == PropertyCategories.ObjectReference)
					{
						if (this.ContainsEdgeToParent(eachColumn) == false)
						{
							edge.ChildNode = this;
							edge.ParentReferenceOnChildNode = eachColumn;
							this.EdgeToParent.Add(eachColumn.PropertyName, edge);
						}
					}
				}
			}
			return this;
		}
		public string GetDbColumnNameFromPropertyName(string propertyName)
		{
			var dbColumnName = this.PrimaryKeys.ContainsKey(propertyName) ?
										this.PrimaryKeys[propertyName].DbColumnName :
										this.DataColumns.ContainsKey(propertyName) ?
											this.DataColumns[propertyName].DbColumnName :
											this.ConcurrencyControl != null && propertyName.Equals(this.ConcurrencyControl.PropertyName) ?
												this.ConcurrencyControl.DbColumnName :
												string.Empty;
			return dbColumnName;
		}
		public void PrintMarkDown(TextWriter writer)
		{
			writer
				.PrintLine($"## CLASS __{this.NodeType.Name}__ as TABLE __{this.DbTableName}__")
				.PrintLine()
				.PrintLine("__Properties__")
				.PrintLine()
				.PrintNodePropertiesAsMarkDown(NodeProperty.C_NODE_PROPERTY_HEADING, this)
				.PrintLine()
				.PrintLine("__Edges__")
				.PrintLine()
				.PrintNodeEdgeAsMarkDown(NodeEdge.C_NODE_EDGE_HEADING, this.EdgeToChildren, this.EdgeToParent)
				.PrintLine()
				.PrintLine(@"---")
				.PrintLine();
		}
		public string[] AllChildEdgeNames()
		{
			return this.EdgeToChildren.Keys.ToArray();
		}
		public NodeEdge[] AllChildEdges()
		{
			return this.EdgeToChildren.Values.ToArray();
		}
		public string[] AllParentEdgeNames()
		{
			return this.EdgeToParent.Keys.ToArray();
		}
		public NodeEdge[] AllParentEdges()
		{
			return this.EdgeToParent.Values.ToArray();
		}
		public NodeProperty[] AllDbColumns
		{
			get
			{
				if (this._allDbColumns == null)
				{
					var allColumns = this.PrimaryKeys.Values
										.Concat(this.DataColumns.Values.Where(p => string.IsNullOrEmpty(p.DbColumnName) == false));
					if (this.ConcurrencyControl != null)
					{
						allColumns = allColumns.Concat(new[] { this.ConcurrencyControl });
					}
					this._allDbColumns = allColumns.ToArray();
				}
				return this._allDbColumns;
			}
		}

		public override int GetHashCode()
		{
			return this.NodeType.GetHashCode();
		}
	}
}