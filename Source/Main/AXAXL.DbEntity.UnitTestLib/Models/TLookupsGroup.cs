using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	[Table("t_lookups_group")]
    public partial class TLookupsGroup
    {
        public TLookupsGroup()
        {
            TLookups = new HashSet<TLookups>();
        }

        [Key]
        [Column("lookups_group_pkey")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LookupsGroupPkey { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("version")]
        [ConcurrencyCheck]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public byte[] Version { get; set; }

        [InverseProperty("LookupsGroupPkeyNavigation")]
        public virtual ICollection<TLookups> TLookups { get; set; }
    }
}
