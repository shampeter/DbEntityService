using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	[Table("t_doc")]
	public class TDoc : ITrackable
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("doc_guid")]
		public int DocGuid { get; set; }

		[Column("owner_type", Order = 2)]
		public string OwnerType { get; set; }

		[Column("owner_guid", Order = 1)]
		public int OwnerGuid { get; set; }

		[Column("filename")]
		public string Filename { get; set; }

		[Column("version")]
		[ConcurrencyCheck]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public byte[] Version { get; set; }
		public EntityStatusEnum EntityStatus { get; set; }
	}
}
