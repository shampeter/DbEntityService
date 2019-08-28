using System;
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
		public NodeEdge CompileChildAddingAction()
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
			this.LogExpression("Child Adding Action", lambda);

			this.ChildAddingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileChildRemoveAction()
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
			this.LogExpression("Child Remove Action", lambda);

			this.ChildRemovingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileParentSettingAction()
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
			this.LogExpression("Parent Setting Action", lambda);

			this.ParentSettingAction = lambda.Compile();

			return this;
		}
		public NodeEdge CompileChildForeignKeyWriters()
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
			this.LogExpression("Foreign Key Setting Action", lambda);

			this.ChildForeignKeyWriter = lambda.Select(l => l.Compile()).ToArray();

			return this;
		}
		public NodeEdge CompileParentPrimaryKeyReaders()
		{
			this.ParentPrimaryKeyReaders =
					this.ParentNodePrimaryKeys.Select(
						p =>
						{
							var lambda = this.ParentNode.CreatePropertyValueReaderFunc(p);
							this.LogExpression("Parent Primary Key Reader", lambda);
							return lambda.Compile();
						}).ToArray();
			return this;
		}
		public NodeEdge CompileChildForeignKeyReaders()
		{
			this.ChildForeignKeyReaders =
				this.ChildNodeForeignKeys.Select(
					p =>
					{
						var lambda = this.ChildNode.CreatePropertyValueReaderFunc(p);
						this.LogExpression("Child Foreign Key Reader", lambda);
						return lambda.Compile();
					}).ToArray();
			return this;
		}
		private void CopyIfNull<P>(NodeEdge another, Func<NodeEdge, P> property, Action<NodeEdge, P> copyOver)
		{
			if (property(this) == null && property(another) != null)
			{
				copyOver(this, property(another));
			}
		}
		internal const string C_NODE_EDGE_TEMPLATE = @"| {0} | {1} | {2} | {3} | {4} | {5} |";
		internal const string C_NODE_EDGE_HEADER_DIVIDER = @"|---|---|---|---|---|---|";
		internal static readonly string[] C_NODE_EDGE_HEADING = new string[] { "Parent", "P. Key", "Child Ref", "Child", "F. Key", "Parent Ref" };
		public string ToMarkDown()
		{
			return string.Format(
				C_NODE_EDGE_TEMPLATE,
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
			);
		}
		[Conditional("DEBUG")]
		private void LogExpression(string message, params Expression[] expressions)
		{
			foreach (var expression in expressions)
			{
				this.Log.LogDebug(
					"{0}: {1} ... {2}", 
					$"Edge.{this.ParentNode.Name}.{this.ChildNode.Name}", 
					message, 
					expression.ToString("C#")
					);
			}
		}
	}
}