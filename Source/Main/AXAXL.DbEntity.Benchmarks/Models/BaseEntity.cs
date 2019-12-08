using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	public class BaseEntity
	{
		[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInserted)]
		[Column("added_dt")]
		public DateTime AddedDt { get; set; }

		// TODO: Need to figure out a way to get the added_by later.
		[ValueInjection(FunctionScript = @"() => ""CLR System""", When = InjectionOptions.WhenInserted)]
		[Column("added_by")]
		public string AddedBy { get; set; }

		// TODO: Need to figure out a way to get the added_app later.
		[ValueInjection(FunctionScript = @"() => ""CLR System""", When = InjectionOptions.WhenInserted)]
		[Column("added_app")]
		public string AddedApp { get; set; }

		[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInsertedAndUpdated)]
		[Column("modify_dt")]
		public DateTime ModifyDt { get; set; }

		[ValueInjection(FunctionScript = @"() => ""CLR System""", When = InjectionOptions.WhenInsertedAndUpdated)]
		[Column("modify_by")]
		public string ModifyBy { get; set; }

		[ValueInjection(FunctionScript = @"() => ""CLR System""", When = InjectionOptions.WhenInsertedAndUpdated)]
		[Column("modify_app")]
		public string ModifyApp { get; set; }

		[ActionInjection(ActionScript = "(a) => ((BaseEntity)a).Version += 1", When = InjectionOptions.WhenUpdated)]
		[ConcurrencyCheck]
		[Column("version")]
		public int Version { get; set; }
	}
}