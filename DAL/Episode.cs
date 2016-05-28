using System;

namespace DAL
{
    public class Episode : DbEntry
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public DateTimeOffset? Date { get; set; }

        public virtual Show Show { set; get; }

        public override bool Equals(object obj)
        {
            Episode e = obj as Episode;
            if (e == null)
            {
                return false;
            }

            return SiteId == e.SiteId;
        }

        public override int GetHashCode()
        {
            return SiteId.GetHashCode();
        }
    }
}
