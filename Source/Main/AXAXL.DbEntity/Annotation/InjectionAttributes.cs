using System;
using System.Collections.Generic;
using System.Data;

namespace AXAXL.DbEntity.Annotation
{
	public enum InjectionOptions
	{
		WhenInserted,
		WhenInsertedAndUpdated,
		WhenUpdated
	}
	public class InjectionAttribute : Attribute
	{
		public InjectionOptions When { get; set; }
	}
}