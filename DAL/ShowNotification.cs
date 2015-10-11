namespace DAL
{
    public class ShowNotification : DbEntry
    {
        public virtual User User { get; set; }
        public virtual Show Show { get; set; }
        public bool Notified { get; set; }
        public ShowNotification()
        {
            Notified = false;
        }
    }
}