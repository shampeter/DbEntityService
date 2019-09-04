using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXAXL.DbEntity.SampleApp.Models
{
	[Table("Author")]
    public class Author : TrackableEntity
	{
		private HashSet<AuthorContact> _authorContacts;
        public Author()
        {
            BookAuthors = new HashSet<BookAuthors>();
			_authorContacts = new HashSet<AuthorContact>();
        }

		[Column("Id")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Key]
        public long Id { get; set; }

		[Column("Name")]
		public string Name { get; set; }

		[InverseProperty(nameof(Models.AuthorContact.Author))]
        public ICollection<AuthorContact> AuthorContacts {
			get => this._authorContacts;
		}

		[InverseProperty(nameof(Models.BookAuthors.Author))]
        public ICollection<BookAuthors> BookAuthors { get; set; }

		[Column("Version")]
		[ConcurrencyCheck]
		public byte[] Version { get; set; }

		public AuthorContact Contact
		{
			get => this._authorContacts.FirstOrDefault();
			set => this._authorContacts.Add(value);
		}
	}
}
