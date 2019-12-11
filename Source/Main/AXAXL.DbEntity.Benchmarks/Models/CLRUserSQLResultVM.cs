using System;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	public class CLRUserSQLResultVM
	{
		public int UserSessionGuid { get; set; }
		public int EventGuid { get; set; }
		public string LockedBy { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTime AddedDt { get; set; }
		public string AddedBy { get; set; }
		public string AddedApp { get; set; }
		public DateTime ModifyDt { get; set; }
		public string ModifyBy { get; set; }
		public string ModifyApp { get; set; }
		public int Version { get; set; }
	}
}
