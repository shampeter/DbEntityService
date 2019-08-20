using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace Entity.Example.Models
{
	[Table("t_company")]
    public partial class TCompany
    {
        public TCompany()
        {
			this.Version = 1;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("company_pkey")]
        public int CompanyPkey { get; set; }

        [Column("company_name")]
        public string CompanyName { get; set; }

        [Column("company_type_fkey")]
        public int CompanyTypeFkey { get; set; }

		[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInserted)]
		[Column("added_dt")]
		public DateTime AddedDate { get; set; }

		[Column("modify_dt"), ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInsertedAndUpdated)]
		public DateTime ModifyDate { get; set; }

		[ValueInjection(FunctionScript = "() => HelperMethods.CurrentUserId", When = InjectionOptions.WhenInserted)]
		[Column("added_by")]
		public string AddedBy { get; set; }

		[ValueInjection(FunctionScript = "() => HelperMethods.CurrentUserId", When = InjectionOptions.WhenInsertedAndUpdated)]
		[Column("modify_by")]
		public string ModifyBy { get; set; }

		[Column("version")]
        [ConcurrencyCheck]
        [ActionInjection(ActionScript = "(a) => ((TCompany)a).Version += 1", When = InjectionOptions.WhenUpdated)]
        public int Version { get; set; }

        [ForeignKey("CompanyTypeFkey")]
        public virtual TLookups CompanyTypeFkeyNavigation { get; set; }
    }
}
