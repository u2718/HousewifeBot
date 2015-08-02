using System.Collections.Generic;

namespace DAL
{
    public class Show : DbEntry
    {
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public virtual List<Episode> Episodes { get; set; } = new List<Episode>();
    }
}
