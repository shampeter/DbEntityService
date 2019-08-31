using System;

namespace AXAXL.DbEntity.Annotation
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ActionInjectionAttribute : InjectionAttribute
	{
		public ActionInjectionAttribute()
		{
		}
		public string ActionScript { get; set; }
		//public string[] ScriptNamespaces { get; set; }
	}

}
