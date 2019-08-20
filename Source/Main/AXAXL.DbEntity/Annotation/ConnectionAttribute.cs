using System;

namespace AXAXL.DbEntity.Annotation
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ConnectionAttribute : Attribute
	{
		public string ConnectionName { get; set; }
	}
}