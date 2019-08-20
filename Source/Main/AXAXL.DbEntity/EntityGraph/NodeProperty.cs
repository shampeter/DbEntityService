using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.EntityGraph
{
	public enum PropertyCategories
	{
		Value,
		Collection,
		ObjectReference
	}
	public enum NodePropertyUpdateOptions
	{
		ByApp,
		ByDbOnInsert,
		ByDbOnInsertAndUpdate,
		ByFwkOnInsert,
		ByFwkOnInsertAndUpdate,
		ByFwkOnUpdate
	}
	public enum NodePropertyUpdateScriptTypes
	{
		None,
		Action,
		Func,
		SqlFunc
	}

	public class NodeProperty
	{
		internal static readonly ScriptOptions defaultScriptOption = ScriptOptions.Default.AddReferences(AppDomain.CurrentDomain.GetAssemblies());
		private ILogger Log { get; set; }
		public NodeProperty(ILogger log)
		{
			this.IsEdge = false;
			this.Log = log;
		}
		public Node Owner { get; set; }
		public string PropertyName { get; set; }
		public Type PropertyType { get; set; }
		public PropertyCategories PropertyCategory { get; set; }
		public string DbColumnName { get; set; }
		public string DbColumnType { get; set; }
		public string ForeignKeyReference { get; set; }
		public string InversePropertyReference { get; set; }
		public NodePropertyUpdateOptions UpdateOption { get; set; }
		public (NodePropertyUpdateScriptTypes ScriptType, string Script, string[] Namespaces) UpdateScript { get; set; }
		public Action<dynamic> ActionInjection { get; set; }
		public Func<dynamic> FuncInjection { get; set; }
		public bool IsEdge { get; set; }
		public Type GetTypeReferencedByEdge()
		{
			if (! this.IsEdge) throw new InvalidOperationException($"Invalid operation. '{this.PropertyName}' is not an edge.");
			Type elementType = null;
			if (this.PropertyCategory == PropertyCategories.Collection)
			{
				Debug.Assert(this.PropertyType.IsGenericType, $"{this.PropertyName} is not a generic collection");
				Debug.Assert(this.PropertyType.GetGenericArguments().Length == 1, $"{this.PropertyName} is a generic collection with more than 1 generic parameter!");
				elementType = this.PropertyType.GetGenericArguments().First();
			}
			else if (this.PropertyCategory == PropertyCategories.ObjectReference)
			{
				elementType = this.PropertyType;
			}
			return elementType;
		}
		public string ToMarkDown()
		{
			return String.Format(
				Node.C_NODE_PROPERTY_TEMPLATE,
				this.Owner.NodeType.Name,
				this.PropertyName,
				this.GetFormatPropertyTypeName(),
				this.PropertyCategory.ToString(),
				this.DbColumnName,
				this.DbColumnType,
				this.UpdateOption,
				string.IsNullOrEmpty(this.UpdateScript.Script) ? string.Empty : this.UpdateScript.ScriptType.ToString(),
				this.UpdateScript.Script
				);
		}
		public NodeProperty CompileScript()
		{
			this.CompileScriptAsync().Wait();
			return this;
		}
		private async Task CompileScriptAsync()
		{
			if (string.IsNullOrEmpty(this.UpdateScript.Script) || this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.SqlFunc || this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.None)
			{
				return;
			}
			var options = defaultScriptOption;
			IEnumerable<string> namespaces = this.UpdateScript.Namespaces ?? new string[0];
			if (namespaces.Contains(this.Owner.NodeType.Namespace) == false)
			{
				namespaces = namespaces.Union(new[] { typeof(Object).Namespace, this.Owner.NodeType.Namespace });
			}
			options = options.AddImports(namespaces);

			if (this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.Action)
			{
				this.ActionInjection = await CSharpScript.EvaluateAsync<Action<dynamic>>(this.UpdateScript.Script, options);
			}
			if (this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.Func)
			{
				this.FuncInjection = await CSharpScript.EvaluateAsync<Func<dynamic>>(this.UpdateScript.Script, options);
			}

			return;
		}
		private string GetFormatPropertyTypeName()
		{
			string formattedTypeName = this.PropertyType.Name;
			if (this.PropertyType.IsGenericType)
			{
				var genericArguments = string.Join(",", this.PropertyType.GenericTypeArguments.Select(p => p.Name));
				formattedTypeName = string.Format("{0}&lt;{1}&gt;", this.PropertyType.Name.Split('`')[0], genericArguments);
			}
			return formattedTypeName;
		}
	}
}