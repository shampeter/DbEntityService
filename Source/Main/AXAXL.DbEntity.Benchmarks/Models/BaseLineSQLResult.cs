using System;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	public class BaseLineSQLResult
	{
		public int EventGuid { get; set; }

		public Nullable<DateTime> DOLFrom { get; set; }

		public Nullable<DateTime> DOLTo { get; set; }

		public string CatstrId { get; set; }

		public string Description { get; set; }

		public string LloydReference { get; set; }

		public Nullable<double> TotalMarketLoss { get; set; }

		public string LockedBy { get; set; }

		public Nullable<DateTime> LockedDt { get; set; }
	}
}
