using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Extensions;
using ExpressionToString;

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
		private static Assembly[] _currentDomainAssemblies = null;
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
		public string[] ForeignKeyReference { get; set; }
		public string InversePropertyReference { get; set; }
		public NodePropertyUpdateOptions UpdateOption { get; set; }
		public (NodePropertyUpdateScriptTypes ScriptType, string Script, string[] Namespaces) UpdateScript { get; set; }
		public Action<dynamic> ActionInjection { get; set; }
		public Func<dynamic> FuncInjection { get; set; }
		public Func<object, IEnumerator<ITrackable>> GetEnumeratorFunc { get; set; }
		internal string GetEnumeratorFuncInString { get; set; }
		public Action<object, object> GetRemoveItemMethodAction { get; set; }
		internal string GetRemoveItemMethodActionInString { get; set; }
		public bool IsEdge { get; set; }
		public bool IsNullable { get; set; }
		public string ConstantValue { get; set; }
		public int Order { get; set; }
		public bool IsConstant => this.PropertyCategory == PropertyCategories.Value && string.IsNullOrEmpty(this.ConstantValue) == false;
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
		public NodeProperty CompileDelegateForHandlingCollection(bool saveExpressionToStringForDebug = false)
		{
			if (this.PropertyCategory == PropertyCategories.Collection)
			{
				var func = this.CreateGetEnumeratorFunc();
				this.GetEnumeratorFuncInString = saveExpressionToStringForDebug ? func.ToString("C#") : null;
				this.GetEnumeratorFunc = func.Compile();
				var act = this.CreateRemoveItemAction();
				this.GetRemoveItemMethodActionInString = saveExpressionToStringForDebug ? act.ToString("C#") : null;
				this.GetRemoveItemMethodAction = act.Compile();
			}
			return this;
		}
		public NodeProperty CompileScript()
		{
			if (string.IsNullOrEmpty(this.UpdateScript.Script) || this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.SqlFunc || this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.None)
			{
				return this;
			}
			if (_currentDomainAssemblies == null)
			{
				// Roslyn script compilation won't work with dynamic assemblies, thus exlcuding by testing IsDynamic 
				_currentDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.IsDynamic == false).ToArray();
			}
			Debug.Assert(_currentDomainAssemblies != null && _currentDomainAssemblies.Length > 0);
			var options = ScriptOptions.Default.AddReferences(_currentDomainAssemblies);
			IEnumerable<string> namespaces = this.UpdateScript.Namespaces ?? new string[0];
			if (namespaces.Contains(this.Owner.NodeType.Namespace) == false)
			{
				namespaces = namespaces.Union(new[] { typeof(Object).Namespace, this.Owner.NodeType.Namespace });
			}
			options = options.AddImports(namespaces);

			if (this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.Action)
			{
				this.ActionInjection = CSharpScript.EvaluateAsync<Action<dynamic>>(this.UpdateScript.Script, options).Result;
			}
			if (this.UpdateScript.ScriptType == NodePropertyUpdateScriptTypes.Func)
			{
				this.FuncInjection = CSharpScript.EvaluateAsync<Func<dynamic>>(this.UpdateScript.Script, options).Result;
			}

			return this;
		}
		internal static readonly string[] C_NODE_PROPERTY_HEADING = 
			new string[] { "Group", "Name", "Type", "Category", "Db Col", "Db Type", "Upd Optn", "Script Type", "Script", "Constant" };
		internal string[] NodePropertyAttributeValues =>
				new string[]
				{
					this.PropertyName,
					this.GetFormattedPropertyTypeName(),
					this.PropertyCategory.ToString(),
					this.DbColumnName,
					this.DbColumnType,
					this.UpdateOption.ToString(),
					string.IsNullOrEmpty(this.UpdateScript.Script) ? string.Empty : this.UpdateScript.ScriptType.ToString(),
					this.UpdateScript.Script,
					this.ConstantValue
				};
		private string GetFormattedPropertyTypeName()
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