using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	public class TCededContractDoc : TDoc
	{
		[Constant("Contract")]
		[Column("owner_type", Order = 2)]
		public string OwnerType { get; set; }

		[InverseProperty(nameof(TCededContract.CededContractDocs))]
		public TCededContract CededContract { get; set; }
	}
}
