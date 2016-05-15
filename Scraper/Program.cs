using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DAL;
using NLog;
using System.Configuration;
using System.Threading.Tasks;

namespace Scraper
{
    class Program
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static int _updateInterval;
        private static bool LoadSettings()
        {
            bool result = true;
            try
            {
                _updateInterval = int.Parse(ConfigurationManager.AppSettings["UpdateInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading update interval");
                result = false;
            }

            return result;
        }

        static void Main()
        {
            Logger.Info($"Scraper started: {Assembly.GetEntryAssembly().Location}");
            if (!LoadSettings()) return;
            var scrapers = new List<Scraper>()
            {
                new LostFilmScraper(@"https://www.lostfilm.tv/browse.php", "http://www.lostfilm.tv/serials.php", GetLastId("lostfilm")),
                new NewStudioScraper(@"http://newstudio.tv/tracker.php", @"http://newstudio.tv/", GetLastId("newstudio"))
            };
            var tasks = new List<Task>(scrapers.Count);
            foreach (var scraper in scrapers)
            {
                var task = new Task(() => LoadShows(scraper));
                tasks.Add(task);
                task.Start();
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static void LoadShows(Scraper scraper)
        {
            UpdateShows(scraper);
            while (true)
            {
                List<Show> shows;
                Logger.Trace($"Retrieving new episodes from {scraper.SiteTitle}");
                try
                {
                    shows = scraper.Load();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"An error occurred while retrieving new episodes from {scraper.SiteTitle}");
                    shows = new List<Show>();
                }
                UpdateEpisodes(shows);
                Thread.Sleep(TimeSpan.FromMinutes(_updateInterval));
            }
        }

        private static void UpdateEpisodes(List<Show> shows)
        {
            int newShowsCount = 0;
            int newEpisodesCount = 0;
            using (var db = new AppDbContext())
            {
                foreach (var show in shows)
                {
                    if (db.Shows.Any(s => s.Title == show.Title))
                    {
                        db.Shows.First(s => s.Title == show.Title)
                            .Episodes.AddRange(show.Episodes);
                    }
                    else
                    {
                        db.Shows.Add(show);
                        newShowsCount++;
                    }
                    newEpisodesCount += show.Episodes.Count;

                    Logger.Info($"{show.Title} - {string.Join(", ", show.Episodes.Select(e => e.Title))}");
                }
                if (newShowsCount > 0)
                {
                    Logger.Info($"{newShowsCount} new {(newShowsCount == 1 ? "show" : "shows")} added");
                }
                if (newEpisodesCount > 0)
                {
                    Logger.Info(
                        $"{newEpisodesCount} new {(newEpisodesCount == 1 ? "episode" : "episodes")} added");
                }
                Logger.Trace("Saving changes to database");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "An error occurred while saving changes to database");
                }
            }
        }

        private static void UpdateShows(Scraper scraper)
        {
            int newShowsCount = 0;
            using (var db = new AppDbContext())
            {
                List<Show> showsList;
                Logger.Debug($"Retrieving shows from {scraper.SiteTitle}");
                try
                {
                    showsList = scraper.LoadShows().Result;
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"An error occurred while retrieving shows from {scraper.SiteTitle}");
                    return;
                }
                try
                {
                    foreach (var show in showsList)
                    {
                        Show dbShow = db.SiteTypes.First(st => st.Name == scraper.SiteTypeName).Shows.FirstOrDefault(s => s.SiteId == show.SiteId);
                        if (dbShow != null)
                        {
                            dbShow.OriginalTitle = show.OriginalTitle;
                            dbShow.Description = show.Description ?? dbShow.Description;
                            dbShow.SiteId = show.SiteId;
                        }
                        else
                        {
                            db.Shows.Add(show);
                            newShowsCount++;
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "An error occurred while adding new shows to database");
                }
                if (newShowsCount > 0)
                {
                    Logger.Info($"{newShowsCount} new {(newShowsCount == 1 ? "show" : "shows")} added");
                }
            }
        }

        private static int GetLastId(string siteTypeName)
        {
            using (var db = new AppDbContext())
            {
                int lastId = 14468;
                Logger.Debug("Retrieving last episode Id from database");
                try
                {
                    lastId = db.Episodes?.Where(e => e.Show.SiteType.Name == siteTypeName).Max(e => (int?)e.SiteId) ?? 14468;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "An error occurred while retrieving last episode Id");
                }
                return lastId;
            }
        }
    }
}
