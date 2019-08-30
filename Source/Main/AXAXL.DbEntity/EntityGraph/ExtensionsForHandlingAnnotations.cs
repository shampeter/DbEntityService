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
using AXAXL.DbEntity.Extensions;
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
		internal static IDictionary<TypeCode, Func<string, (bool Success, object Converted)>> _constantValueParserMap = new Dictionary<TypeCode, Func<string, (bool, object)>>
		{
			[TypeCode.Boolean] = (string input) => { bool output; var success = Boolean.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Byte] = (string input) => { byte output; var success = Byte.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Char] = (string input) => { char output; var success = Char.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.DateTime] = (string input) => { DateTime output; var success = DateTime.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Decimal] = (string input) => { decimal output; var success = Decimal.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Double] = (string input) => { double output; var success = Double.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Int16] = (string input) => { short output; var success = Int16.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Int32] = (string input) => { int output; var success = Int32.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.Int64] = (string input) => { long output; var success = Int64.TryParse(input, out output); return (success, (object)output); },
			[TypeCode.String] = (string input) => (true, input),
			[TypeCode.Single] = (string input) => { Single output; var success = Single.TryParse(input, out output); return (success, (object)output); }
			// [TypeCode.DBNull] = (string input) => { bool output; var success = DBNull.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.Empty] = (string input) => { bool output; var success = Empty.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.Object] = (string input) => { bool output; var success = Object.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.SByte] = (string input) => { bool output; var success = SByte.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.UInt16] = (string input) => { bool output; var success = UInt16.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.UInt32] = (string input) => { bool output; var success = UInt32.TryParse(input, out output); return (success, (object)output); },
			// [TypeCode.UInt64] = (string input) => { bool output; var success = UInt64.TryParse(input, out output); return (success, (object)output); }
		};
		internal static NodeProperty HandleConstantAttribute(this NodeProperty node, PropertyInfo property)
		{
			var constantAttribute = property.GetCustomAttribute<ConstantAttribute>();
			if (constantAttribute == null)
			{
				// no constant attribute.  Skip.
				return node;
			}
			var constantValue = constantAttribute.Value;
			Debug.Assert(string.IsNullOrEmpty(constantValue) == false, $"Constant value assigned to {property.Name} cannot be blank.");

			var type = property.PropertyType;
			var underlyingType = Nullable.GetUnderlyingType(type);
			type = underlyingType ?? type;

			var converted = _constantValueParserMap[Type.GetTypeCode(type)].Invoke(constantValue);
			Debug.Assert(converted.Success, $"Cannot convert constant value {constantValue} into {property.PropertyType.Name}");

			node.ConstantValue = converted.Converted;
			node.ConstantValueSetterAction = property.SetValue;

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
		internal static NodePropertyUpdateOptions DetermineColumnUpdateOption(this Attribute attr)
		{
			var option = NodePropertyUpdateOptions.ByApp;
			if (typeof(DatabaseGeneratedAttribute).IsAssignableFrom(attr.GetType()))
			{
				DatabaseGeneratedAttribute columnAttr = attr as DatabaseGeneratedAttribute;
				switch (columnAttr.DatabaseGeneratedOption)
				{
					case DatabaseGeneratedOption.Computed:
						option = NodePropertyUpdateOptions.ByDbOnInsert | NodePropertyUpdateOptions.ByDbOnUpdate;
						break;
					case DatabaseGeneratedOption.Identity:
						option = NodePropertyUpdateOptions.ByDbOnInsert;
						break;
				}
			}
			else if (typeof(InjectionAttribute).IsAssignableFrom(attr.GetType()))
			{
				var argWhen = (attr as InjectionAttribute).When;
				switch (argWhen)
				{
					case InjectionOptions.WhenInserted:
						option = NodePropertyUpdateOptions.ByFwkOnInsert;
						break;
					case InjectionOptions.WhenInsertedAndUpdated:
						option = NodePropertyUpdateOptions.ByFwkOnInsert | NodePropertyUpdateOptions.ByFwkOnUpdate;
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
		internal static TextWriter PrintNodePropertiesAsMarkDown(this TextWriter writer, string[] headings, Node node)
		{
			var primaryKeys = node.PrimaryKeys;
			var dataColumns = node.DataColumns;
			var versionColumn = node.ConcurrencyControl;

			var buffer = new List<string[]>();
			buffer.AddRange(primaryKeys?.Values.Select(p => new[] { "P.Key" }.Concat(p.NodePropertyAttributeValues).ToArray()));
			buffer.AddRange(dataColumns?.Values.Select(p => new[] { "Data" }.Concat(p.NodePropertyAttributeValues).ToArray()));
			if (versionColumn != null)
			{
				buffer.Add(new[] { "Version" }.Concat(versionColumn.NodePropertyAttributeValues).ToArray());
			}

			writer
				.PrintMarkDownTable(headings, buffer)
				.PrintLine()
				;

			var exprToPrint = node.DataColumns.Values
								.Where
								(
									d => string.IsNullOrEmpty(d.GetEnumeratorFuncInString) == false ||
										string.IsNullOrEmpty(d.GetRemoveItemMethodActionInString) == false
								)
								.ToArray();

			foreach(var eachProp in exprToPrint)
			{
				writer.PrintMarkDownCSharp($"{eachProp.PropertyName}: Getting Enumerator", eachProp.GetEnumeratorFuncInString);
				writer.PrintMarkDownCSharp($"{eachProp.PropertyName}: Remove Method", eachProp.GetRemoveItemMethodActionInString);
			}

			return writer;
		}
		internal static TextWriter PrintNodeEdgeAsMarkDown(this TextWriter writer, string[] headings, IDictionary<string, NodeEdge> edgesToChild, IDictionary<string, NodeEdge> edgesToParent)
		{
			var buffer = new List<string[]>();
			if (edgesToChild != null && edgesToChild.Count > 0)
			{
				buffer.AddRange(edgesToChild.Values.Select(p => new[] { "To Child" }.Concat(p.NodeEdgeAttributeValues).ToArray()));
			}
			if (edgesToParent != null && edgesToParent.Count > 0)
			{
				buffer.AddRange(edgesToParent.Values.Select(p => new[] { "To Parent" }.Concat(p.NodeEdgeAttributeValues).ToArray()));
			}
			writer
				.PrintMarkDownTable(headings, buffer)
				.PrintLine();

			foreach(var toChild in edgesToChild.Values)
			{
				writer
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Action To Add Child", toChild.ChildAddingActionInString)
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Action to Remove Child", toChild.ChildRemovingActionInString)
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Action to Set Parent", toChild.ParentSettingActionInString)
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Write Child Foreign Keys", toChild.ChildForeignKeyWriterInString)
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Read Parent Primary Keys", toChild.ParentPrimaryKeyReadersInString)
					.PrintMarkDownCSharp($"{toChild.ParentNode.Name} &rarr; {toChild.ChildNode.Name} : Read Child Foreign Keys", toChild.ChildForeignKeyReadersInString)
					.PrintLine()
					;
			}

			foreach (var toParent in edgesToParent.Values)
			{
				writer
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Action To Add Child", toParent.ChildAddingActionInString)
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Action to Remove Child", toParent.ChildRemovingActionInString)
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Action to Set Parent", toParent.ParentSettingActionInString)
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Write Child Foreign Keys", toParent.ChildForeignKeyWriterInString)
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Read Parent Primary Keys", toParent.ParentPrimaryKeyReadersInString)
					.PrintMarkDownCSharp($"{toParent.ParentNode.Name} &rarr; {toParent.ChildNode.Name} : Read Child Foreign Keys", toParent.ChildForeignKeyReadersInString)
					.PrintLine()
					;
			}

			return writer;
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