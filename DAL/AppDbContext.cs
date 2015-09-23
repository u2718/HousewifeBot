using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using FuzzyString;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }

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

        public Show GetShowById(int id)
        {
            return Shows.FirstOrDefault(s => s.Id == id);
        }

        public List<Show> GetShowsFuzzy(string paramTitle)
        {
            List<Show> _shows = new List<Show>();

            List<FuzzyStringComparisonOptions> options = new List<FuzzyStringComparisonOptions>();

            options.Add(FuzzyStringComparisonOptions.UseHammingDistance);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubsequence);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubstring);

            FuzzyStringComparisonTolerance tolerance = FuzzyStringComparisonTolerance.Normal;
            foreach (Show show in Shows)
            {
                if (show.OriginalTitle.ApproximatelyEquals(paramTitle, options, tolerance) || show.Title.ApproximatelyEquals(paramTitle, options, tolerance))
                {
                    _shows.Add(show);
                }
            }

            return _shows;
        }
    }
}
