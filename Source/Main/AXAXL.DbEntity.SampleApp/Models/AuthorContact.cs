using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models
{
	[Table("AuthorContact")]
    public class AuthorContact : TrackableEntity
	{
		[Column("Id")]

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Key]
		public long Id { get; set; }

		[Column("AuthorId")]
		public long AuthorId { get; set; }

		[Column("ContactNumber")]
		public string ContactNumber { get; set; }

		[Column("Address")]
		public string Address { get; set; }

		[ForeignKey(nameof(AuthorId))]
		[InverseProperty(nameof(Models.Author.AuthorContacts))]
        public Author Author { get; set; }

		[Column("Version")]
		[ConcurrencyCheck]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public RowVersion Version { get; set; }
	}
}
