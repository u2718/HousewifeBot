using System;
using System.Collections.Generic;

namespace DAL
{
    public class Subscription : DbEntry
    {
        public int UserId { get; set; }
        public int ShowId { get; set; }

        public virtual User User { get; set; }
        public virtual Show Show { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public DateTimeOffset SubscriptionDate { get; set; }
    }
}
