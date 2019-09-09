using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models
{
	[Table("Publisher")]
    public class Publisher : TrackableEntity
	{
        public Publisher()
        {
            Books = new HashSet<Book>();
        }

		[Column("Id")]
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

		[Column("Name")]
		public string Name { get; set; }

		[InverseProperty(nameof(Models.Book.Publisher))]
        public ICollection<Book> Books { get; set; }

		[Column("Version")]
		[ConcurrencyCheck]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public RowVersion Version { get; set; }
	}
}
