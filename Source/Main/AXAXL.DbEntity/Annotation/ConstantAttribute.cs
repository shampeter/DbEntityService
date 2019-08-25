using System;

namespace AXAXL.DbEntity.Annotation
{
	public class ConstantAttribute : Attribute
	{
		public string Value { get; set; }
		public ConstantAttribute() : base() { }
		public ConstantAttribute(string value)
		{
			this.Value = value;
		}
	}
}
