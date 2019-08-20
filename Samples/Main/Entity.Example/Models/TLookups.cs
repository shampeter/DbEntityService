using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entity.Example.Models
{
	[Table("t_lookups")]
    public partial class TLookups
    {
        public TLookups()
        {
        }
        [Key]
        [Column("lookups_pkey")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LookupsPkey { get; set; }
        [Column("lookups_group_pkey")]
        public int LookupsGroupPkey { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("version")]
        [ConcurrencyCheck]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public byte[] Version { get; set; }

        [ForeignKey("LookupsGroupPkey")]
        [InverseProperty("TLookups")]
        public virtual TLookupsGroup LookupsGroupPkeyNavigation { get; set; }
    }
}
