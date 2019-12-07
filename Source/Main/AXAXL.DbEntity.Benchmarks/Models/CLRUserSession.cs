using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	[Table("t_clr_user_session")]
	public class CLRUserSession : BaseEntity
	{
		[Key]
		[Column("user_session_guid")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int UserSessionGuid { get; set; }

		[Column("event_guid")]
		public int EventGuid { get; set; }

		[Column("locked_by")]
		public string LockedBy { get; set; }

		[Column("log_on_dt")]
		public Nullable<DateTime> LogOnDt { get; set; }

		[Column("log_off_dt")]
		public Nullable<DateTime> LogOffDt { get; set; }

		[Column("comments")]
		public string Comments { get; set; }

		[ForeignKey(nameof(EventGuid))]
		[InverseProperty(nameof(Event.CLRUserSessionList))]
		public Event EventUserWorkedOn { get; set; }

		[ForeignKey(nameof(LockedBy))]
		public UserPrincipal LockedByUser { get; set; }
	}
}
