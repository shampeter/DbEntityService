﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	[Table("t_ceded_contract_layer")]
    public partial class TCededContractLayer : ITrackable
    {
		private TLookups layerType;
		public TCededContractLayer()
		{
			this.CededContractLayerDocs = new List<TCededContractLayerDoc>();
		}

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ceded_contract_layer_pkey")]
		public int CededContractLayerPkey { get; set; }

        [Column("ceded_contract_fkey")]
        public int CededContractFkey { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("attachment_point", TypeName = "money")]
        public decimal? AttachmentPoint { get; set; }

        [Column("layer_type_fkey")]
        public int? LayerTypeFkey { get; set; }

        [Column("limit", TypeName = "money")]
        public decimal? Limit { get; set; }

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
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		[Column("version")]
        public RowVersion Version { get; set; }

        [ForeignKey(nameof(CededContractFkey))]
        [InverseProperty(nameof(TCededContract.CededContractLayers))]
        public virtual TCededContract CededContract { get; set; }

        [ForeignKey("LayerTypeFkey")]
        public TLookups LayerType {
			get => this.layerType;
			set
			{
				this.LayerTypeFkey = value.LookupsPkey;
				this.layerType = value;
			}
		}

		[InverseProperty(nameof(TCededContractLayerDoc.CededContractLayer))]
		public IList<TCededContractLayerDoc> CededContractLayerDocs { get; set; }
		public EntityStatusEnum EntityStatus { get; set; }
	}
}
