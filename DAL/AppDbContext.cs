using System;
using System.Data.Entity;
using System.Linq;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public AppDbContext() : base("DbConnection")
        {

        }

        public User GetUserByTelegramId(int telegramId)
        {
            return Users.FirstOrDefault(e => e.TelegramUserId == telegramId);
        }

        public Show GetShowByTitle(string title)
        {
            title = title.ToLower();
            return Shows.FirstOrDefault(s => s.Title.ToLower() == title ||
                                             s.OriginalTitle.ToLower() == title ||
                                             s.Title.ToLower() + " (" + s.OriginalTitle + ")" == title);
        }
    }
}
