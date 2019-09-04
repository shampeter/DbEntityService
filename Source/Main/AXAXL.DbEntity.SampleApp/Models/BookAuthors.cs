using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXAXL.DbEntity.SampleApp.Models
{
	[Table("BookAuthors")]
    public class BookAuthors : TrackableEntity
	{
		[Column("BookId")]
		[Key]
        public long BookId { get; set; }
		[Column("AuthorId")]
		[Key]
        public long AuthorId { get; set; }

		[InverseProperty(nameof(Models.Author.BookAuthors))]
		[ForeignKey(nameof(AuthorId))]
        public virtual Author Author { get; set; }

		[InverseProperty(nameof(Models.Book.BookAuthors))]
		[ForeignKey(nameof(BookId))]
        public virtual Book Book { get; set; }

		[Column("Version")]
		[ConcurrencyCheck]
		public byte[] Version { get; set; }
	}
}
