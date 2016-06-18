using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    public class Show : DbEntry
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Description { get; set; }
        public int SiteTypeId { get; set; }

        public virtual ICollection<Episode> Episodes { get; set; } = new List<Episode>();
        public virtual ICollection<Subscription> Subscription { get; set; } = new List<Subscription>();
        public virtual ICollection<ShowNotification> ShowNotifications { get; set; } = new List<ShowNotification>();
        public virtual SiteType SiteType { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset? DateCreated { get; set; }

        public override bool Equals(object obj)
        {
            Show s = obj as Show;
            if (s == null)
            {
                return false;
            }

            return SiteId == s.SiteId;
        }

        public override int GetHashCode()
        {
            return SiteId.GetHashCode();
        }
    }
}
