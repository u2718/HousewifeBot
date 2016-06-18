namespace DAL
{
    public class ShowNotification : DbEntry
    {
        public int UserId { get; set; }
        public int ShowId { get; set; }

        public virtual User User { get; set; }
        public virtual Show Show { get; set; }
        public bool Notified { get; set; }
        public ShowNotification()
        {
            Notified = false;
        }
    }
}