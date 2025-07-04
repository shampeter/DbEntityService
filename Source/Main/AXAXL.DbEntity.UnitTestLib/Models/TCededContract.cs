﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	[Table("t_ceded_contract")]
    public class TCededContract : ITrackable
    {
		private TCompany xlCompany;
		private TCompany cedant;
        public TCededContract()
        {
            CededContractLayers = new List<TCededContractLayer>();
			CededContractDocs = new List<TCededContractDoc>();
        }

		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ceded_contract_pkey")]
        public int CededContractPkey { get; set; }

        [Column("ceded_contract_num")]
        [ValueInjection(FunctionScript = "() => NextIntSequence(1, 1)", When = InjectionOptions.WhenInserted, ServiceName = "AXAXL.DbEntity.UnitTestLib.Models.INextSequence, AXAXL.DbEntity.UnitTestLib")]
        public int CededContractNum { get; set; }

        [Column("uw_year")]
        public int UwYear { get; set; }

        [Column("creation_date", TypeName = "datetime")]
        public DateTime CreationDate { get; set; }

        [Column("cedant_company_fkey")]
        public int CedantCompanyFkey { get; set; }

        [Column("xl_company_fkey")]
        public int XlCompanyFkey { get; set; }

		[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInserted)]
		[Column("added_dt")]
        public DateTime AddedDate { get; set; }

		[ValueInjection(FunctionScript = "() => DateTime.Now", When = InjectionOptions.WhenInsertedAndUpdated)]
        [Column("modify_dt")]
        public DateTime ModifyDate { get; set; }

		[ValueInjection(FunctionScript = "() => HelperMethods.CurrentUserId", When = InjectionOptions.WhenInserted)]
		[Column("added_by")]
        public string AddedBy { get; set; }

		[ValueInjection(FunctionScript = "() => HelperMethods.CurrentUserId", When = InjectionOptions.WhenInsertedAndUpdated)]
		[Column("modify_by")]
        public string ModifyBy { get; set; }

        [ConcurrencyCheck]
        [Column("version")]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public RowVersion Version { get; set; }

        [ForeignKey(nameof(CedantCompanyFkey))]
        public TCompany CedantCompany {
			get => this.cedant;
			set
			{
				this.CedantCompanyFkey = value.CompanyPkey;
				this.cedant = value;
			}
		}

        [ForeignKey(nameof(XlCompanyFkey))]
        public TCompany XlCompany
		{
			get => this.xlCompany;
			set
			{
				this.XlCompanyFkey = value.CompanyPkey;
				this.xlCompany = value;
			}
		}

		[InverseProperty(nameof(TCededContractLayer.CededContract))]
		public IList<TCededContractLayer> CededContractLayers { get; set; }

		private const string C_FOREIGN_KEY_NAMES = nameof(TCededContractDoc.OwnerGuid) + "," + nameof(TCededContractDoc.OwnerType);

		[ForeignKey(C_FOREIGN_KEY_NAMES)]
		[InverseProperty(nameof(TCededContractLayer.CededContract))]
		public IList<TCededContractDoc> CededContractDocs { get; set; }

		public EntityStatusEnum EntityStatus { get; set; }
    }
}
