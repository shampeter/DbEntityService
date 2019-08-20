using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Extensions
{
	public static class GeneralExtensions
	{
		private static readonly string NL = Environment.NewLine;
		public static dynamic ToDynamic(this IDictionary<string, object> argDictionary)
		{
			if (argDictionary == null || argDictionary.Count <= 0)
			{
				return null;
			}
			var expandable = new ExpandoObject();
			var dict = (IDictionary<string, object>)expandable;

			foreach (var eachKeyValue in argDictionary)
			{
				dict.Add(eachKeyValue.Key, eachKeyValue.Value);
			}
			if (expandable.Count() <= 0)
			{
				throw new InvalidOperationException("Result dynamic has 0 property");
			}
			return expandable;
		}
		public static string ToMarkDown(this Expression argExpr)
		{
			var buffer = new StringBuilder();
			if (argExpr is LambdaExpression)
			{
				var lambda = (LambdaExpression)argExpr;
				var parameters = string.Format(
					@"({0}) => {{",
					string.Join(
						',',
						lambda.Parameters.Select(p => p.ToString()).ToArray()
					));
				buffer
				   .Append(@"```c#").Append(NL)
				   .Append(parameters).Append(NL)
				   .Append(lambda.Body.ToMarkDown())
				   .Append(@"}").Append(NL)
				   .Append(@"```").Append(NL)
				   ;
			}
			else if (argExpr is BlockExpression)
			{
				var blocks = (BlockExpression)argExpr;
				var variables = blocks.Variables;
				if (variables != null && variables.Count > 0)
				{
					foreach(var variable in variables)
					{
						buffer.Append(variable).Append(';').Append(NL);
					}
				}
				foreach (var eachExpr in blocks.Expressions)
				{
					buffer.Append(eachExpr.ToMarkDown()).Append(NL);
				}
			}
			else
			{
				buffer
					.Append(argExpr.ToString()).Append(';');
			}
			return buffer.ToString();
		}
	}
}
