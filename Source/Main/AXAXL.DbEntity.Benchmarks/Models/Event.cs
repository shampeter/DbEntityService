using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	[Table("t_event")]
	public class Event
	{
		public Event()
		{
			this.EventTotalMarketLossList = new List<EventTotalMarketLoss>();
			this.CLRUserSessionList = new List<CLRUserSession>();
		}
		[Key]
		[Column("event_guid")]
		public int EventGuid { get; set; }

		[Column("dt_of_loss_from")]
		public Nullable<DateTime> DOLFrom { get; set; }

		[Column("dt_of_loss_to")]
		public Nullable<DateTime> DOLTo { get; set; }

		[Column("catstr_id")]
		public string CatstrId { get; set; }

		[Column("description")]
		public string Description { get; set; }

		[Column("loss_location_name")]
		public string LossLocationName { get; set; }

		[Column("segment_cat_ind")]
		public bool? IsSegmentCat { get; set; }

		[Column("long_description")]
		public string LongDescription { get; set; }

		[Column("active_ind")]
		public bool IsActive { get; set; }

		[Column("lloyd_reference")]
		public string LloydReference { get; set; }

		[ForeignKey(nameof(EventTotalMarketLoss.EventGuid))]
		public IList<EventTotalMarketLoss> EventTotalMarketLossList { get; set; }

		[ForeignKey(nameof(CLRUserSession.EventGuid))]
		[InverseProperty(nameof(CLRUserSession.EventUserWorkedOn))]
		public IList<CLRUserSession> CLRUserSessionList { get; set; }
	}
}