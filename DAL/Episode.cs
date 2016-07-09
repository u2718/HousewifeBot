using System;
using System.Collections.Generic;

namespace DAL
{
    public class Episode : DbEntry
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public DateTimeOffset? Date { get; set; }
        public int ShowId { get; set; }

        public virtual Show Show { set; get; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public override bool Equals(object obj)
        {
            Episode e = obj as Episode;
            if (e == null)
            {
                return false;
            }

            if (SeasonNumber.HasValue && EpisodeNumber.HasValue && e.SeasonNumber.HasValue && e.EpisodeNumber.HasValue)
            {
                return SeasonNumber == e.SeasonNumber && EpisodeNumber == e.EpisodeNumber;
            }

            return SiteId == e.SiteId;
        }

        public override int GetHashCode()
        {
            if (SeasonNumber.HasValue && EpisodeNumber.HasValue)
            {
                return 31*SeasonNumber.Value + EpisodeNumber.Value;
            }

            return SiteId.GetHashCode();
        }
    }
}
