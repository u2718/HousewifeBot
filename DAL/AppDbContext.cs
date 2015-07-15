using System.Data.Entity;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Series> Series { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public AppDbContext() : base("DbConnection")
        {
            
        }
    }
}
