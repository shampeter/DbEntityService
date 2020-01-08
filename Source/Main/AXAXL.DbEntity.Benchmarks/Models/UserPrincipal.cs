using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Annotation;

namespace AXAXL.DbEntity.Benchmarks.Models
{
	[Table("t_sec_principal")]
	public class UserPrincipal
	{
		private string loginNameInLowerCase;

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("user_guid")]
		public int UserGuid { get; set; }

		[Key]
		[Column("login_name")]
		public string LoginName { 
			get
			{
				return this.loginNameInLowerCase;
			}
			set
			{
				this.loginNameInLowerCase = value.ToUpper();
			}
		}

		[Column("principal_type")]
		public string PrincipalType { get; set; }

		[Column("last_name")]
		public string LastName { get; set; }

		[Column("first_name")]
		public string FirstName { get; set; }

		[Column("middle_initial")]
		public string MiddleInitial { get; set; }

		[Column("bus_role")]
		public string BusRole { get; set; }

		[Column("title")]
		public string Title { get; set; }

		[Column("active_ind")]
		public bool ActiveInd { get; set; }
	}
}
