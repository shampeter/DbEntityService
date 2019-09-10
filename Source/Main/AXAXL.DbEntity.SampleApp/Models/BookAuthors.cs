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

		public static IEqualityComparer<BookAuthors> _equalityComparer = new BookAuthors.BookAuthorsComparer();

		public class BookAuthorsComparer : IEqualityComparer<BookAuthors>
		{
			public bool Equals(BookAuthors x, BookAuthors y)
			{
				return x.AuthorId == y.AuthorId && x.BookId == y.BookId;
			}

			public int GetHashCode(BookAuthors obj)
			{
				var hashCode = 0;
				if (obj != null)
				{
					hashCode = HashCode.Combine<long, long>(obj.AuthorId, obj.BookId);
				}
				return hashCode;
			}
		}
	}
}
