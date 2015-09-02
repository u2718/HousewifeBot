namespace DAL
{
    public class Notification : DbEntry
    {
        public virtual Subscription Subscription { get; set; }
        public virtual Episode Episode { get; set; }
        public bool Notified { get; set; }

        public Notification()
        {
            Notified = false;
        }
    }
}
