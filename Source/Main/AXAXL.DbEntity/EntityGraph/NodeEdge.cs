using System;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Extensions;
using ExpressionToString;

namespace AXAXL.DbEntity.EntityGraph
{
	public class NodeEdge
	{
		private ILogger Log { get; set; }
		public NodeEdge(ILogger log)
		{
			this.Log = log;
		}
		public Node ParentNode { get; set; }
		public NodeProperty[] ParentNodePrimaryKeys { get; set; }
		public NodeProperty ChildReferenceOnParentNode { get; set; }
		public Node ChildNode { get; set; }
		public NodeProperty[] ChildNodeForeignKeys { get; set; }
		public NodeProperty ParentReferenceOnChildNode { get; set; }
		public Action<object, IEnumerable<object>> ChildAddingAction { get; set; }
		public Action<object, object> ChildRemovingAction { get; set; }
		public Action<object, object> ParentSettingAction { get; set; }
		public Action<object, object>[] ChildForeignKeyWriter { get; set; }
		public Func<object, dynamic>[] ParentPrimaryKeyReaders { get; set; }
		public Func<object, dynamic>[] ChildForeignKeyReaders { get; set; }
		public string ChildAddingActionInString { get; set; }
		public string ChildRemovingActionInString { get; set; }
		public string ParentSettingActionInString { get; set; }
		public string[] ChildForeignKeyWriterInString { get; set; }
		public string[] ParentPrimaryKeyReadersInString { get; set; }
		public string[] ChildForeignKeyReadersInString { get; set; }

		public NodeEdge Merge(NodeEdge another)
		{
			this.CopyIfNull(another, (p) => p.ParentNode, (n, p) => n.ParentNode = p);
			this.CopyIfNull(another, (p) => p.ParentNodePrimaryKeys, (n, p) => n.ParentNodePrimaryKeys = p);
			this.CopyIfNull(another, (p) => p.ChildReferenceOnParentNode, (n, p) => n.ChildReferenceOnParentNode = p);
			this.CopyIfNull(another, (p) => p.ChildNode, (n, p) => n.ChildNode = p);
			this.CopyIfNull(another, (p) => p.ChildNodeForeignKeys, (n, p) => n.ChildNodeForeignKeys = p);
			this.CopyIfNull(another, (p) => p.ParentReferenceOnChildNode, (n, p) => n.ParentReferenceOnChildNode = p);

			return this;
		}
		public NodeEdge SortKeysByOrder()
		{
			Debug.Assert(this.ParentNodePrimaryKeys != null);
			Debug.Assert(this.ChildNodeForeignKeys != null);
			if (this.ParentNodePrimaryKeys.Length > 1)
			{
				this.ParentNodePrimaryKeys = this.ParentNodePrimaryKeys.OrderBy(p => p.Order).ToArray();
			}
			if (this.ChildNodeForeignKeys.Length > 1)
			{
				this.ChildNodeForeignKeys = this.ChildNodeForeignKeys.OrderBy(p => p.Order).ToArray();
			}
			return this;
		}
		public NodeEdge CompileChildAddingAction(bool saveExpressionToStringForDebug = false)
		{
			Expression<Action<object, IEnumerable<object>>> lambda = null;

			if (this.ChildReferenceOnParentNode != null)
			{
				lambda = this.ChildReferenceOnParentNode.CreateCollectionFillingAction();
			}
			else
			{
				this.Log.LogDebug("Creating empty child adding action because there is no child set reference.");
				lambda = this.CreateEmptyCollectionFillingAction();
			}
			if (saveExpressionToStringForDebug)
			{
				this.ChildAddingActionInString = lambda.ToString("C#");
			}
			this.ChildAddingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileChildRemoveAction(bool saveExpressionToStringForDebug = false)
		{
			Expression<Action<object, object>> lambda = null;

			if (this.ChildReferenceOnParentNode != null)
			{
				lambda = this.ChildReferenceOnParentNode.CreateCollectionRemovingAction();
			}
			else
			{
				this.Log.LogDebug("Creating empty child adding action because there is no child set reference.");
				lambda = this.CreateEmptyObjectAssignmentAction();
			}
			if (saveExpressionToStringForDebug)
			{
				this.ChildRemovingActionInString = lambda.ToString("C#");
			}
			this.ChildRemovingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileParentSettingAction(bool saveExpressionToStringForDebug = false)
		{
			Expression<Action<object, object>> lambda = null;

			if (this.ParentReferenceOnChildNode != null)
			{
				lambda = this.ParentReferenceOnChildNode.CreateObjectAssignmentAction();
			}
			else
			{
				this.Log.LogDebug("Creating empty parent setting action because there is no parent reference.");
				lambda = this.CreateEmptyObjectAssignmentAction();
			}
			if (saveExpressionToStringForDebug)
			{
				this.ParentSettingActionInString = lambda.ToString("C#");
			}

			this.ParentSettingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileChildForeignKeyWriters(bool saveExpressionToStringForDebug = false)
		{
			Expression<Action<object, object>>[] lambda = null;
			if (this.ChildNodeForeignKeys != null && this.ChildNodeForeignKeys.Length > 0)
			{
				lambda = this.ChildNodeForeignKeys.Select(p => p.CreateObjectAssignmentAction()).ToArray();
			}
			else
			{
				this.Log.LogDebug("Creating empty parent setting action because there is no parent reference.");
				lambda = this.ChildNodeForeignKeys.Select(p => this.CreateEmptyObjectAssignmentAction()).ToArray();
			}
			if (saveExpressionToStringForDebug)
			{
				this.ChildForeignKeyWriterInString = lambda.Select(l => l.ToString("C#")).ToArray();
			}

			this.ChildForeignKeyWriter = lambda.Select(l => l.Compile()).ToArray();

			return this;
		}
		public NodeEdge CompileParentPrimaryKeyReaders(bool saveExpressionToStringForDebug = false)
		{
			var lambda = this.ParentNodePrimaryKeys
								.Select(p => this.ParentNode.CreatePropertyValueReaderFunc(p))
								.ToArray();
			if (saveExpressionToStringForDebug)
			{
				this.ParentPrimaryKeyReadersInString = lambda.Select(l => l.ToString("C#")).ToArray();
			}
			this.ParentPrimaryKeyReaders = lambda.Select(l => l.Compile()).ToArray();

			return this;
		}
		public NodeEdge CompileChildForeignKeyReaders(bool saveExpressionToStringForDebug = false)
		{
			var lambda = this.ChildNodeForeignKeys
								.Select(p => this.ChildNode.CreatePropertyValueReaderFunc(p))
								.ToArray();
			if (saveExpressionToStringForDebug)
			{
				this.ChildForeignKeyReadersInString = lambda.Select(l => l.ToString("C#")).ToArray();
			}
			this.ChildForeignKeyReaders = lambda.Select(l => l.Compile()).ToArray();

			return this;
		}
		private void CopyIfNull<P>(NodeEdge another, Func<NodeEdge, P> property, Action<NodeEdge, P> copyOver)
		{
			if (property(this) == null && property(another) != null)
			{
				copyOver(this, property(another));
			}
		}
		internal static readonly string[] C_NODE_EDGE_HEADING = new string[] {"Direction", "Parent", "P. Key", "Child Ref", "Child", "F. Key", "Parent Ref" };
		internal string[] NodeEdgeAttributeValues =>
			new string[]
			{
				this.ParentNode != null ? this.ParentNode.NodeType.Name : string.Empty,
				string.Join(
					", ",
					this.ParentNodePrimaryKeys != null ? this.ParentNodePrimaryKeys.Select(p => p.PropertyName).ToArray() : new string[0]
				),
				this.ChildReferenceOnParentNode != null ? this.ChildReferenceOnParentNode.PropertyName : String.Empty,
				this.ChildNode != null ? this.ChildNode.NodeType.Name : String.Empty,
				string.Join(
					", ",
					this.ChildNodeForeignKeys != null ? this.ChildNodeForeignKeys.Select(p => p.PropertyName).ToArray() : new string[0]
				),
				this.ParentReferenceOnChildNode != null ? this.ParentReferenceOnChildNode.PropertyName : string.Empty
			};
	}
}