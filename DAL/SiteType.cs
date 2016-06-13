using System.Collections.Generic;

namespace DAL
{
    public class SiteType : DbEntry
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Show> Shows { get; set; }
    }
}