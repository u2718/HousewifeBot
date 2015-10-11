using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    public class Show : DbEntry
    {
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public virtual List<Episode> Episodes { get; set; } = new List<Episode>();
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset? DateCreated { get; set; }
    }
}
