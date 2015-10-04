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
        public DbSet<Settings> Settings { get; set; }
        public DbSet<DownloadTask> DownloadTasks { get; set; }

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
            List<Tuple<Show, double>> shows = new List<Tuple<Show, double>>();

            List<FuzzyStringComparisonOptions> options = new List<FuzzyStringComparisonOptions>();

            options.Add(FuzzyStringComparisonOptions.UseHammingDistance);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubsequence);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubstring);
            Func<string, string, double> calculateSimilarityFactor = (source, target) =>
            {
                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                {
                    return 0;
                }

                double factor =(double)(source.LongestCommonSubsequence(target).Length + source.LongestCommonSubstring(target).Length)/source.Length;
                if (source.Length == target.Length)
                {
                    factor += (double) (source.Length - source.HammingDistance(target))/source.Length;
                    factor /= 3;
                }
                else
                {
                    factor /= 2;
                }
                return factor;
            };

            FuzzyStringComparisonTolerance tolerance = FuzzyStringComparisonTolerance.Normal;
            foreach (Show show in Shows)
            {
                if (show.OriginalTitle.ApproximatelyEquals(paramTitle, options, tolerance) || show.Title.ApproximatelyEquals(paramTitle, options, tolerance))
                {
                    double maxSimilarityFactor = Math.Max(calculateSimilarityFactor(show.OriginalTitle, paramTitle), calculateSimilarityFactor(show.Title, paramTitle));
                    shows.Add(new Tuple<Show, double>(show, maxSimilarityFactor));
                }
            }

            return shows.OrderByDescending(s => s.Item2)
                .Select(s => s.Item1)
                .ToList();
        }

        public Settings GetSettingsByUser(User user)
        {
            return Settings.FirstOrDefault(s => s.User.Id == user.Id);
        }

        public Notification GetNotificationById(int notificationId)
        {
            return Notifications.FirstOrDefault(n => n.Id == notificationId);
        }
    }
}
