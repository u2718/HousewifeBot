namespace DAL
{
    public class Notification : DbEntry
    {
        public int SubscriptionId { get; set; }
        public int EpisodeId { get; set; }

        public virtual Subscription Subscription { get; set; }
        public virtual Episode Episode { get; set; }
        public bool Notified { get; set; }

        public Notification()
        {
            Notified = false;
        }
    }
}
