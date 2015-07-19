using System;

namespace DAL
{
    public class Subscription : DbEntry
    {
        public virtual User User { get; set; }
        public virtual Show Show { get; set; }
        public DateTime SubscriptionDate { get; set; }
    }
}
