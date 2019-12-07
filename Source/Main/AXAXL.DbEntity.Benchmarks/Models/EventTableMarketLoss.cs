using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	[Table("t_event_total_marketloss")]
	public class EventTotalMarketLoss : BaseEntity
	{
		[Column("event_guid")]
		public int EventGuid { get; set; }

		[Column("total_market_loss", TypeName = "decimal(19,2)")]
		public Nullable<double> TotalMarketLoss { get; set; }
	}
}