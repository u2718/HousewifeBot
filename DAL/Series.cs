using System;

namespace DAL
{
    public class Series : DbEntry
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public DateTimeOffset? Date { get; set; }

        public virtual Show Show { set; get; }
    }
}
