using System.Collections.Generic;

namespace DAL
{
    public sealed class Show : DbEntry
    {
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public List<Episode> Episodes { get; set; }

        public Show()
        {
             Episodes = new List<Episode>();
        }
    }
}
