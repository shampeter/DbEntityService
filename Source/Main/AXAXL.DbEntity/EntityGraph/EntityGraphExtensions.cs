using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using AXAXL.DbEntity.Interfaces;
using System.Text;
using System.Dynamic;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.EntityGraph
{
	public static class EntityGraphExtensions
	{
		private static readonly string NL = Environment.NewLine;
		public static Expression<Func<object, dynamic>> CreatePropertyValueReaderFunc(this Node node, NodeProperty column)
		{
			var inputParameter = Expression.Parameter(typeof(object), "entity");
			var entityProperty = Expression.Convert(
									Expression.Property(
										Expression.Convert(inputParameter, node.NodeType),
										column.PropertyName
										),
									typeof(object)
								);
			var block = Expression.Block(new[] { entityProperty });
			var lambda = Expression.Lambda<Func<object, dynamic>>(block, new[] { inputParameter });

			return lambda;
		}
		public static Expression<Action<object, IEnumerable<object>>> CreateEmptyCollectionFillingAction(this NodeEdge edge)
		{
			var label = Expression.Label(typeof(void), "return");
			// (entity, parent) => { return }
			var lambda = Expression.Lambda<Action<object, IEnumerable<object>>>(
				Expression.Block(Expression.Label(label)),
				new[] {
					Expression.Parameter(typeof(object), "entity"),
					Expression.Parameter(typeof(IEnumerable<object>), "childSet")
				});
			return lambda;
		}
		public static Expression<Action<object, IEnumerable<object>>> CreateCollectionFillingAction(this NodeProperty aCollection)
		{
			Debug.Assert(aCollection != null);
			var childRefPropertyType = aCollection.PropertyType;
			var clearMethod = SearchMethodOnType(childRefPropertyType, @"Clear"); 
			Debug.Assert(clearMethod != null, $"No Clear() method can be found on '{aCollection.PropertyName}' of '{aCollection.Owner.Name}'");
			var addMethodParameterType = GetMethodParameterType(childRefPropertyType);
			var addMethod = SearchMethodOnType(childRefPropertyType, @"Add", addMethodParameterType);
			Debug.Assert(addMethod != null, $"No Add() method can be found on '{aCollection.PropertyName}' of '{aCollection.Owner.Name}'");

			Expression<Func<IEnumerable<object>, int>> returnListCount = l => l.Count();
			Expression<Func<IEnumerable<object>, int, object>> fetchListElement = (l, i) => l.ElementAt(i);
			var constantOne = Expression.Constant(1);
			var entityInput = Expression.Parameter(typeof(object), "entity");
			var childSetInput = Expression.Parameter(typeof(IEnumerable<object>), "childSet");
			var childCount = Expression.Parameter(typeof(int), "childCount");
			var runCount = Expression.Parameter(typeof(int), "runCount");
			var property = Expression.Parameter(childRefPropertyType, "property");
			var breakLabel = Expression.Label(typeof(void));

			var block = Expression.Block(
				new[] { childCount, runCount, property },
				// property = entity.childreference
				Expression.Assign(
					property,
					Expression.Property(
						Expression.Convert(entityInput, aCollection.Owner.NodeType),
						aCollection.PropertyName
					)),
				// property.Clear()
				Expression.Call(
					property,
					clearMethod
				),
				// childCount = (c) => c.Count 
				Expression.Assign(
					childCount,
					Expression.Invoke(returnListCount, childSetInput)
				),
				// runCount = 0;
				Expression.Assign(runCount, Expression.Constant(0)),
				// loop
				Expression.Loop(
					Expression.IfThenElse(
						// If (runCount >- childCount) break
						Expression.GreaterThanOrEqual(runCount, childCount),
						Expression.Break(breakLabel),
						// else { property.Add((type)(childSet, runCount) => childSet[runCount]); runCount += 1 }
						Expression.Block(
							Expression.Call(
								property,
								addMethod,
								Expression.Convert(
									Expression.Invoke(fetchListElement, childSetInput, runCount),
									addMethodParameterType[0]
								)
							),
							Expression.AddAssign(runCount, constantOne)
						)
					)
				),
				Expression.Label(breakLabel)
			);
			var lambda = Expression.Lambda<Action<object, IEnumerable<object>>>(block, new[] { entityInput, childSetInput });
			return lambda;
		}
		public static Expression<Action<object, object>> CreateEmptyObjectAssignmentAction(this NodeEdge edge)
		{
			var label = Expression.Label(typeof(void), "return");
			// (entity, parent) => { return }
			var lambda = Expression.Lambda<Action<object, object>>(
				Expression.Block( Expression.Label(label) ), 
				new[] {
					Expression.Parameter(typeof(object), "entity"),
					Expression.Parameter(typeof(object), "parent")
				});
			return lambda;
		}
		public static Expression<Action<object, object>> CreateObjectAssignmentAction(this NodeProperty property)
		{
			Debug.Assert(property != null);
			var entityInput = Expression.Parameter(typeof(object), "entity");
			var valueInput = Expression.Parameter(typeof(object), "input");
			var block = Expression.Block(
				// ((childnode_type)entity).parent_reference = (parent_node_type)parent
				Expression.Assign(
					Expression.Property(
						Expression.Convert(entityInput, property.Owner.NodeType),
						property.PropertyName
						),
					Expression.Convert(valueInput, property.PropertyType)
					)
				);
			var lambda = Expression.Lambda<Action<object, object>>(block, new [] { entityInput, valueInput });

			return lambda;
		}
		public static Expression<Func<object, IEnumerator<ITrackable>>> CreateGetEnumeratorFunc(this NodeProperty property)
		{
			var enumeratorMethod = SearchMethodOnType(property.PropertyType, "GetEnumerator");
			Debug.Assert(enumeratorMethod != null, $"Failed to locate 'GetEnumerator' method on {property.PropertyName} of {property.Owner.Name}");
			var input = Expression.Parameter(typeof(object), "entity");
			var block = Expression.Block(
				Expression.Convert(
					Expression.Call(
						Expression.Property(
							Expression.Convert(input, property.Owner.NodeType),
							property.PropertyName
						),
						enumeratorMethod
					),
					typeof(IEnumerator<ITrackable>)
				)
			);
			var lambda = Expression.Lambda<Func<object, IEnumerator<ITrackable>>>(block, new[] { input });
			return lambda;
		}
		public static Expression<Action<object, object>> CreateRemoveItemAction(this NodeProperty property)
		{
			var removeMethod = SearchMethodOnType(property.PropertyType, "Remove");
			var entityInput = Expression.Parameter(typeof(object), "entity");
			var elementToBeRemoved = Expression.Parameter(typeof(object), "toBeRemoved");
			var block = Expression.Block(
					Expression.Call(
						Expression.Property(
							Expression.Convert(entityInput, property.Owner.NodeType),
							property.PropertyName
						),
						removeMethod,
						Expression.Convert(
							elementToBeRemoved,
							GetMethodParameterType(property.PropertyType).FirstOrDefault()
						)
					)
				);
			var lambda = Expression.Lambda<Action<object, object>>(block, new[] { entityInput, elementToBeRemoved });
			return lambda;
		}
		public static NodeProperty IdentifyMember<TEntity>(this Node node, Expression<Func<TEntity, dynamic>> memberExpression) where TEntity : class
		{
			var member = memberExpression.Body as MemberExpression;
			Debug.Assert(member != null);
			var property = member.Member as PropertyInfo;
			Debug.Assert(property != null);

			return node.GetPropertyFromNode(property.Name);
		}
		private static Type[] GetMethodParameterType(Type type, bool onlyFirstArgument = true)
		{
			return type.IsGenericType ? (onlyFirstArgument ? new Type[]{ type.GenericTypeArguments.FirstOrDefault() } : type.GenericTypeArguments) : new Type[] { typeof(object) };
		}
		private static MethodInfo SearchMethodOnType(Type type, string methodName, params Type[] methodParameterTypes)
		{
			var types = methodParameterTypes ?? new Type[0];
			var methodInfo = type.GetRuntimeMethod(methodName, types) ?? type.GetInterfaces().Select(i => i.GetRuntimeMethod(methodName, types)).Where(i => i != null).FirstOrDefault();
			return methodInfo;
		}
		/* Tested original POC code for loop child set and adding it to parent's child collection.
		private static Action<object, IEnumerable<object>> ChildSetFeedAction(Type type)
		{
			var propInfo = type.GetProperty("MyList");
			var propType = propInfo.PropertyType;
			var clearMethod = propType.GetInterfaces().Select(i => i.GetRuntimeMethod("Clear", new Type[0])).Where(i => i != null).FirstOrDefault();
			var genericparam = propType.GetGenericArguments().FirstOrDefault();
			var addMethod = propType.GetInterfaces().Select(i => i.GetRuntimeMethod("Add", new[] { genericparam })).Where(i => i != null).FirstOrDefault();

			Expression<Func<IEnumerable<object>, int>> listCount = l => l.Count();
			Expression<Func<IEnumerable<object>, int, object>> fetch = (l, i) => l.ElementAt(i);
			var constantOne = Expression.Constant(1);
			var entityInput = Expression.Parameter(typeof(object), "entity");
			var childInput = Expression.Parameter(typeof(IEnumerable<object>), "child");
			var childCount = Expression.Parameter(typeof(int), "childCount");
			var runCount = Expression.Parameter(typeof(int), "runCount");
			var property = Expression.Parameter(propType, "property");
			var breakLabel = Expression.Label(typeof(void));

			var block = Expression.Block(
				new[] { childCount, runCount, property },
				Expression.Assign(
					property,
					Expression.Property(
						Expression.Convert(entityInput, type),
						"MyList"
					)),
				Expression.Call(
					property,
					clearMethod
				),
				Expression.Assign(
					childCount,
					Expression.Invoke(listCount, childInput)
				),
				Expression.Assign(runCount, Expression.Constant(0)),
				Expression.Loop(
					Expression.IfThenElse(
						Expression.GreaterThanOrEqual(runCount, childCount),
						Expression.Break(breakLabel),
						Expression.Block(
							Expression.Call(
								property,
								addMethod,
								Expression.Convert(
									Expression.Invoke(fetch, childInput, runCount),
									genericparam
								)
							),
							Expression.AddAssign(runCount, constantOne)
						)
					)
				),
				Expression.Label(breakLabel)
			);
			var lambda = Expression.Lambda<Action<object, IEnumerable<object>>>(block, new[] { entityInput, childInput });
			return lambda.Compile();
		}
		*/
	}
}
