using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.EntityGraph;
using ExpressionToString;


namespace AXAXL.DbEntity.MSSql
{
	internal class QueryExtensionForSqlOperators : IQueryExtensionForSqlOperators
	{
		#region IQueryExtensionForSqlOperators implementation

		public bool IsSupported(MethodCallExpression method) => _translations.ContainsKey(method.Method);

		public (Expression LeftNode, string SqlOperator, Expression RightNode) Translate
			(MethodCallExpression methodCall) => _translations[methodCall.Method].Invoke(methodCall);

		#endregion

		#region Private and internal implementation

		// Note that the date format is not taking care of time zone.
		internal const string C_DATE_FORMAT_FOR_SQL = "yyyy-MM-dd HH:mm:ss.fff";
		private static readonly Type _tyEnumOfInt = typeof(IEnumerable<int>);
		private static readonly Type _tyEnumOfLong = typeof(IEnumerable<long>);
		private static readonly Type _tyEnumOfDateTime = typeof(IEnumerable<DateTime>);
		private static readonly Type _tyEnumOfString = typeof(IEnumerable<String>);

		private static IDictionary<MethodInfo, Func<MethodCallExpression, (Expression LeftNode, string SqlOperator, Expression RightNode)>> _translations
			= new Dictionary<MethodInfo, Func<MethodCallExpression, (Expression LeftNode, string SqlOperator, Expression RightNode)>>
			{
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.Like))] = SqlLikeOpTranslate,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(int), _tyEnumOfInt })] = SqlInOpNumberTranslate<int>,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(int?), _tyEnumOfInt })] = SqlInOpNumberTranslate<int>,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(long), _tyEnumOfLong })] = SqlInOpNumberTranslate<long>,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(long?), _tyEnumOfLong })] = SqlInOpNumberTranslate<long>,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(DateTime), _tyEnumOfDateTime })] = SqlInOpQuotedDatesTranslate,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(DateTime?), _tyEnumOfDateTime })] = SqlInOpQuotedDatesTranslate,
				[typeof(IQueryExtensions).GetMethod(nameof(IQueryExtensions.In), new[] { typeof(string), _tyEnumOfString })] = SqlInOpQuotedStringTranslate,
			};
		private static (Expression LeftNode, string SqlOperator, Expression RightNode) TranslateInOp(MethodCallExpression m, Expression convertToCommaDelimited)
		{
			var translate = Expression.Invoke(convertToCommaDelimited, m.Arguments[1]);
			var block = Expression.Block(new[] { translate });
			var resultingLIst = Expression.Lambda<Func<string>>(block).Compile().Invoke();

			return (m.Arguments[0], @" IN (" + resultingLIst + ")", null);
		}
		private static (Expression LeftNode, string SqlOperator, Expression RightNode) SqlInOpQuotedDatesTranslate(MethodCallExpression m)
		{
			Expression<Func<IEnumerable<DateTime>, string>> convert = dates => BuildCommaDelimitedDateInString(dates);
			return TranslateInOp(m, convert);
		}
		private static (Expression LeftNode, string SqlOperator, Expression RightNode) SqlInOpQuotedStringTranslate(MethodCallExpression m)
		{
			Expression<Func<IEnumerable<string>, string>> convert = strings => BuildCommaDelimitedString(strings);
			return TranslateInOp(m, convert);
		}

		private static (Expression LeftNode, string SqlOperator, Expression RightNode) SqlInOpNumberTranslate<T>(MethodCallExpression m)
		{
			Expression<Func<IEnumerable<T>, string>> convert = numbers => BuildCommaDelimitedNumbersInString<T>(numbers);
			return TranslateInOp(m, convert);
		}

		private static (Expression LeftNode, string SqlOperator, Expression RightNode) SqlLikeOpTranslate(MethodCallExpression m)
		{
			return (m.Arguments[0], @" LIKE ", m.Arguments[1]);
		}

		private static string BuildCommaDelimitedDateInString(IEnumerable<DateTime> dates)
		{
			return string.Join(",", dates.Select(d => $"'{d.ToString(C_DATE_FORMAT_FOR_SQL)}'"));
		}
		private static string BuildCommaDelimitedString(IEnumerable<string> strings)
		{
			return string.Join(",", strings.Select(s => $"'{s}'"));
		}
		private static string BuildCommaDelimitedNumbersInString<T>(IEnumerable<T> numbers)
		{
			return string.Join(", ", numbers);
		}
		/* Clever code instead of the various version of SqlIn### and BuildComma### methods.
		 * Insight drawn from https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
		 * really kind of crazy but so damn clever.  Guess it will take someone including me a while to understand this.
		 * So better no use this in production code.  But it's so damn fun.
		 * 
			private static (Expression LeftNode, string SqlOperator, Expression RightNode) SqlInOpTranslate(MethodCallExpression m)
			{	
				var enumElmType = m.Arguments[1].Type.GenericTypeArguments[0];
				Func<object, string> constructed = (Func<object, string>)typeof(QueryExtensionForSqlOperators)
								.GetMethod("Combine", BindingFlags.Static | BindingFlags.NonPublic)
								.MakeGenericMethod(enumElmType)
								.CreateDelegate(typeof(Func<object, string>))
								;
				Expression<Func<object, string>> convert = (obj) => constructed(obj);
				var translate = Expression.Invoke(convert, m.Arguments[1]);
				var block = Expression.Block(new[] { translate });
				var resultingLIst = Expression.Lambda<Func<string>>(block).Compile().Invoke();

				return (m.Arguments[0], @" IN (" + resultingLIst + ")", null);
			}
			private static string Combine<T>(object obj)
			{
				IEnumerable<T> list = (IEnumerable<T>)obj;
				string commaDelimited = null;
				if (typeof(T).IsAssignableFrom(typeof(string)) || typeof(T).IsAssignableFrom(typeof(DateTime)))
				{
					commaDelimited = string.Join(",", list.Select(l => $"'{l}'"));
				}
				else
				{
					commaDelimited = string.Join(",", list);
				}

				return commaDelimited;
			}
		*/

		#endregion
	}
}

