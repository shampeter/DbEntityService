using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	public class TCededContractLayerDoc : TDoc
	{

		[Constant("Layer")]
		[Column("owner_type", Order = 2)]
		public string OwnerType { get; set; }
	}
}
