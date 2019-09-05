using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models
{
	[Table("Book")]
    public class Book : TrackableEntity
	{
        public Book()
        {
            BookAuthors = new HashSet<BookAuthors>();
        }

		[Column("Id")]
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

		[Column("Title")]
		public string Title { get; set; }

		[Column("CategoryId")]
		public long CategoryId { get; set; }

		[Column("PublisherId")]
        public long PublisherId { get; set; }

		[ForeignKey(nameof(CategoryId))]
		[InverseProperty(nameof(BookCategory.Books))]
        public BookCategory Category { get; set; }

		[ForeignKey(nameof(PublisherId))]
		[InverseProperty(nameof(Models.Publisher.Books))]
        public Publisher Publisher { get; set; }

		[InverseProperty(nameof(Models.BookAuthors.Book))]
        public ICollection<BookAuthors> BookAuthors { get; set; }

		[Column("Version")]
		[ConcurrencyCheck]
		public Timestamp Version { get; set; }
	}
}
