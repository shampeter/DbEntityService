using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.MSSql.Autofac
{
	public class MSSqlGeneratorResponseCache : IInterceptor
	{
		public const string C_MS_SQL_GENERATOR_CACHE_INTERCEPTOR_NAME = @"SqlGeneratorCache";
		public class MethodSelectionHookForSQLGenCache : IProxyGenerationHook
		{
			public void MethodsInspected()
			{
			}

			public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
			{
			}

			public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
			{
				var methodName = methodInfo.Name;
				return MSSqlGeneratorResponseCache._methodResponseToBeCached.ContainsKey(methodName);
			}
		}

		internal static readonly IDictionary<string, Func<string, IInvocation, string>> _methodResponseToBeCached = new Dictionary<string, Func<string, IInvocation, string>>
		{
			[nameof(IMSSqlGenerator.ExtractPrimaryKeyAndConcurrencyControlColumns)] = SignatureForExtractPrimaryKeyAndConcurrencyControlColumns,
			[nameof(IMSSqlGenerator.CreateSelectComponent)] = SignatureForCreateSelectComponent,
			[nameof(IMSSqlGenerator.CreateSqlParameters)] = SignatureForCreateSqlParameters,
			[nameof(IMSSqlGenerator.CreateWhereClause)] = SignatureForCreateWhereClause,
			[nameof(IMSSqlGenerator.CreateOutputComponent)] = SignatureForCreateOutputComponent,
			[nameof(IMSSqlGenerator.CreateUpdateAssignmentComponent)] = SignatureForCreateUpdateAssignmentComponent,
			[nameof(IMSSqlGenerator.CreateInsertComponent)] = SignatureForCreateInsertComponent,
			[nameof(IMSSqlGenerator.CreateDeleteClause)] = SignatureForCreateDeleteClause,
			[nameof(IMSSqlGenerator.CreatePropertyValueReaderMap)] = SignatureForCreatePropertyValueReaderMap
		};

		private ILogger Log { get; set; }
		private IMemoryCache Cache { get; set; }

		public MSSqlGeneratorResponseCache(ILoggerFactory factory, IMemoryCache cache)
		{
			this.Log = factory.CreateLogger<MSSqlGeneratorResponseCache>();
			this.Cache = cache;
		}
		public void Intercept(IInvocation invocation)
		{
			var name = invocation.Method.Name;

			this.Log.LogDebug("Intercepting method {0}", name);

			bool proceed = true;
			string signature = null;
			if (_methodResponseToBeCached.ContainsKey(name))
			{
				signature = _methodResponseToBeCached[name].Invoke(name, invocation);
				object value;
				bool cacheFound = this.Cache.TryGetValue(signature, out value);
				if (cacheFound)
				{
					invocation.ReturnValue = value;
					proceed = false;
				}
				this.Log.LogDebug("{1} cache by signature: {0}", signature, (cacheFound ? "Found" : "No"));
			}
			if (proceed)
			{
				invocation.Proceed();
				if (signature != null)
				{
					this.Cache.Set(signature, invocation.ReturnValue);
					this.Log.LogDebug("Caching result by signature: {0}", signature);

				}
			}
		}
		private const string C_NA = @"NA";
		private static string SignatureForCreatePropertyValueReaderMap(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			var columnsSign = SignatureForNodeProperties(invocation.GetArgumentValue(1));

			return $"{methodName}:{nodeSign},{columnsSign}";
		}

		private static string SignatureForCreateDeleteClause(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			return $"{methodName}:{nodeSign}";
		}

		private static string SignatureForCreateInsertComponent(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			return $"{methodName}:{nodeSign}";
		}

		private static string SignatureForCreateUpdateAssignmentComponent(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			return $"{methodName}:{nodeSign}";
		}

		private static string SignatureForCreateOutputComponent(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			var boolSign = invocation.GetArgumentValue(1).ToString();
			return $"{methodName}:{nodeSign},{boolSign}";
		}

		private static string SignatureForCreateWhereClause(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			var columnsSign = SignatureForNodeProperties(invocation.GetArgumentValue(1));
			var prefixSign = invocation.GetArgumentValue(2).ToString();
			return $"{methodName}:{nodeSign},{columnsSign},{prefixSign}";
		}

		private static string SignatureForCreateSqlParameters(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			var columnsSign = SignatureForNodeProperties(invocation.GetArgumentValue(1));
			var prefixSign = invocation.GetArgumentValue(2).ToString();
			return $"{methodName}:{nodeSign},{columnsSign},{prefixSign}";
		}

		private static string SignatureForCreateSelectComponent(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			var maxRowSign = invocation.GetArgumentValue(1).ToString();
			return $"{methodName}:{nodeSign},{maxRowSign}";
		}

		private static string SignatureForExtractPrimaryKeyAndConcurrencyControlColumns(string methodName, IInvocation invocation)
		{
			var nodeSign = SignatureForNode(invocation.GetArgumentValue(0));
			return $"{methodName}:{nodeSign}";
		}
		private static string SignatureForNode(object obj)
		{
			Node node = obj as Node;
			Debug.Assert(node != null);
			var nodeSign = node.Name;
			return nodeSign;
		}
		private static string SignatureForNodeProperties(object obj)
		{
			NodeProperty[] columns = obj as NodeProperty[];
			Debug.Assert(columns != null);
			var columnsSign = string.Join('+', columns.Select(c => c.PropertyName));
			return columnsSign;
		}
	}
}
