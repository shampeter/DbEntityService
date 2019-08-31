using System;

namespace AXAXL.DbEntity.Annotation
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ValueInjectionAttribute : InjectionAttribute
	{
		public ValueInjectionAttribute()
		{
		}
		public string FunctionScript { get; set; }
		//public string[] ScriptNamespaces { get; set; }
		//public string SQLFunction { get; set; }
	}
}
