using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models
{
	public class TrackableEntity : ITrackable
	{
		public EntityStatusEnum EntityStatus { get; set; }
	}
}
