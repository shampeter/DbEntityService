using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.EntityGraph
{
	internal static class ExtensionsForHandlingAnnotations
	{
		internal static Node HandleTableAttribute(this Node node)
		{
			TableAttribute nodeAttr = node.NodeType.GetCustomAttribute<TableAttribute>();
			if (nodeAttr == null) return node;

			node.DbTableName = nodeAttr.Name;
			node.DbSchemaName = nodeAttr.Schema;

			return node;
		}
		internal static Node HandleConnectionAttribute(this Node node)
		{
			ConnectionAttribute nodeAttr = node.NodeType.GetCustomAttribute<ConnectionAttribute>();
			if (nodeAttr == null) return node;

			node.DbConnectionName = nodeAttr.ConnectionName;
			return node;
		}
		internal static NodeProperty HandleColumnAttribute(this NodeProperty node, PropertyInfo property, IDatabaseDriver driver)
		{
			ColumnAttribute columnAttr = property.GetCustomAttribute<ColumnAttribute>();
			if (columnAttr == null) return node;

			string dbType = driver.GetSqlDbType(node.PropertyType);
			if (! string.IsNullOrEmpty(columnAttr.TypeName))
			{
				dbType = columnAttr.TypeName;
			}
			node.DbColumnName = columnAttr.Name;
			node.DbColumnType = dbType;
			node.Order = columnAttr.Order;
			return node;
		}
		internal static NodeProperty HandleConstantAttribute(this NodeProperty node, PropertyInfo property)
		{
			node.ConstantValue = property.GetCustomAttribute<ConstantAttribute>()?.Value;
			return node;
		}
		internal static NodeProperty HandleDataGenerationAttribute(this NodeProperty node, PropertyInfo property)
		{
			DatabaseGeneratedAttribute columnAttr = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
			if (columnAttr == null) return node;

			node.UpdateOption = columnAttr.DetermineColumnUpdateOption();
			return node;
		}
		internal static NodeProperty HandleValueInjectionAttribute(this NodeProperty node, PropertyInfo property)
		{
			ValueInjectionAttribute columnAttr = property.GetCustomAttribute<ValueInjectionAttribute>();
			if (columnAttr == null) return node;
			var updateScript = !string.IsNullOrEmpty(columnAttr.FunctionScript) ?
							(NodePropertyUpdateScriptTypes.Func, columnAttr.FunctionScript, columnAttr.ScriptNamespaces) :
							(NodePropertyUpdateScriptTypes.SqlFunc, columnAttr.SQLFunction, null);
			node.UpdateScript = updateScript;
			node.UpdateOption = columnAttr.DetermineColumnUpdateOption();
			return node;
		}
		internal static NodeProperty HandleActionInjectionAttribute(this NodeProperty node, PropertyInfo property)
		{
			ActionInjectionAttribute columnAttr = property.GetCustomAttribute<ActionInjectionAttribute>();
			if (columnAttr == null) return node;
			node.UpdateScript = (NodePropertyUpdateScriptTypes.Action, columnAttr.ActionScript, columnAttr.ScriptNamespaces);
			node.UpdateOption = columnAttr.DetermineColumnUpdateOption();
			return node;
		}
		internal static readonly char[] C_FOREIGN_KEY_SEPARATORS = new char[] { ',', ' ' };
		internal static NodeProperty HandleForeignKeyAttribute(this NodeProperty node, PropertyInfo property)
		{
			var fKeyAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
			string[] fKeyPropNames = null;
			if (fKeyAttr != null)
			{
				// foreign key on parent node can name compound foreign key on child node in comma-separated names.
				fKeyPropNames = fKeyAttr.Name.Split(C_FOREIGN_KEY_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
				if (fKeyPropNames.Length == 0)
				{
					fKeyPropNames = null;
				}
			}

			Debug.Assert(fKeyAttr == null || fKeyPropNames != null , $"Name of ForeignKeyAttribute on {node.PropertyName} is blank");

			node.ForeignKeyReference = fKeyPropNames;

			return node;
		}
		internal static NodeProperty HandleInversePropertyAttribute(this NodeProperty node, PropertyInfo property)
		{
			var invPropAttr = node.Owner.NodeType.GetProperty(node.PropertyName).GetCustomAttribute<InversePropertyAttribute>();
			var invPropName = invPropAttr != null ? invPropAttr.Property : string.Empty;

			Debug.Assert(invPropAttr == null || !string.IsNullOrEmpty(invPropName), $"Property of InversePropertyAttribute on {node.PropertyName} is blank");

			node.InversePropertyReference = invPropName;

			return node;
		}

/*
		internal static string ForeignKeyReference(this NodeProperty node)
		{
			var fKeyAttr = node.Owner.NodeType.GetProperty(node.PropertyName).GetCustomAttribute<ForeignKeyAttribute>();
			var fKeyPropName = fKeyAttr != null ? fKeyAttr.Name : string.Empty;

			Debug.Assert(fKeyAttr == null || !string.IsNullOrEmpty(fKeyPropName), $"Name of ForeignKeyAttribute on {node.PropertyName} is blank");

			return fKeyPropName;
		}
		internal static string InversePropertyReference(this NodeProperty node)
		{
			var invPropAttr = node.Owner.NodeType.GetProperty(node.PropertyName).GetCustomAttribute<InversePropertyAttribute>();
			var invPropName = invPropAttr != null ? invPropAttr.Property : string.Empty;

			Debug.Assert(invPropAttr == null || !string.IsNullOrEmpty(invPropName), $"Property of InversePropertyAttribute on {node.PropertyName} is blank");

			return invPropName;
		}
*/
		internal static NodeProperty FindForeignKeyForParentReferenceOnChild(this Node node, string parentRefPropNameOnChild)
		{
			Debug.Assert(string.IsNullOrEmpty(parentRefPropNameOnChild) == false, $"Parameter {nameof(parentRefPropNameOnChild)} of method {nameof(FindForeignKeyForParentReferenceOnChild)} cannot be blank or null");
			NodeProperty parentRefPropOnChild = null;
			NodeProperty foreignKey = null;
			var found = node.DataColumns.TryGetValue(parentRefPropNameOnChild, out parentRefPropOnChild);
			Debug.Assert(found, $"Property {parentRefPropNameOnChild} is not found class {node.NodeType.Name}");
			var attr = parentRefPropOnChild.PropertyType.GetCustomAttribute<ForeignKeyAttribute>();
			if (attr == null)
			{
				var foreignKeysPointingToParentRefProperty = node.DataColumns.Values
					.Where(c =>
					{
						var foreignKeyAttr = c.PropertyType.GetCustomAttribute<ForeignKeyAttribute>();
						return (foreignKeyAttr != null && foreignKeyAttr.Name.EqualsIgnoreCase(parentRefPropNameOnChild));
					})
					.ToArray();
				Debug.Assert(foreignKeysPointingToParentRefProperty.Length > 0, $"No foreign key defined for {parentRefPropNameOnChild} in class {node.NodeType.Name}");
				foreignKey = foreignKeysPointingToParentRefProperty.First();
			}
			else
			{
				found = node.DataColumns.TryGetValue(attr.Name, out foreignKey);
				Debug.Assert(found, $"No foreign key defined for {parentRefPropNameOnChild} with name {attr.Name} in class {node.NodeType.Name}");
			}
			return foreignKey;
		}
		internal static bool IsEntity(this Type target)
		{
			return target.GetCustomAttribute<TableAttribute>() != null;
		}
		internal static NodePropertyUpdateOptions DetermineColumnUpdateOption(this Attribute argAttr)
		{
			var option = NodePropertyUpdateOptions.ByApp;
			if (typeof(DatabaseGeneratedAttribute).IsAssignableFrom(argAttr.GetType()))
			{
				DatabaseGeneratedAttribute columnAttr = argAttr as DatabaseGeneratedAttribute;
				switch (columnAttr.DatabaseGeneratedOption)
				{
					case DatabaseGeneratedOption.Computed:
						option = NodePropertyUpdateOptions.ByDbOnInsertAndUpdate;
						break;
					case DatabaseGeneratedOption.Identity:
						option = NodePropertyUpdateOptions.ByDbOnInsert;
						break;
				}
			}
			else if (typeof(InjectionAttribute).IsAssignableFrom(argAttr.GetType()))
			{
				var argWhen = (argAttr as InjectionAttribute).When;
				switch (argWhen)
				{
					case InjectionOptions.WhenInserted:
						option = NodePropertyUpdateOptions.ByFwkOnInsert;
						break;
					case InjectionOptions.WhenInsertedAndUpdated:
						option = NodePropertyUpdateOptions.ByFwkOnInsertAndUpdate;
						break;
					case InjectionOptions.WhenUpdated:
						option = NodePropertyUpdateOptions.ByFwkOnUpdate;
						break;
				}
			}

			return option;
		}
		internal static bool IsPrimaryKey(this PropertyInfo argProperty) => argProperty.GetCustomAttribute<KeyAttribute>() != null;
		internal static bool IsConcurrencyCheck(this PropertyInfo argProperty) => argProperty.GetCustomAttribute<ConcurrencyCheckAttribute>() != null;
		internal static void PrintMarkDown(this IDictionary<string, NodeProperty> dictionary, TextWriter writer)
		{
			foreach (var p in dictionary.Values)
			{
				p.PrintMarkDown(writer);
			}
		}
		internal static void PrintMarkDown(this IDictionary<string, NodeEdge> dictionary, TextWriter writer)
		{
			foreach (var p in dictionary.Values)
			{
				p.PrintMarkDown(writer);
			}
		}

		// =>
		// (
		// 	argProperty.PropertyCategory == PropertyCategories.Collection
		// 	|| 
		// 	argProperty.PropertyCategory == PropertyCategories.ObjectReference
		// )
		// &&
		// ( 
		// 	argProperty.PropertyType.GetCustomAttribute<ForeignKeyAttribute>() != null 
		// 	|| 
		// 	argProperty.PropertyType.GetCustomAttribute<InversePropertyAttribute>() != null
		// );
	}

}