using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DAL;
using NLog;

namespace Scraper
{
    public class Program
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Dictionary<string, int> LastStoredEpisodesId = new Dictionary<string, int>() { { "lostfilm", 14468 }, { "newstudio", 19131 } };

        private static int updateInterval;
                    
        public static void Main()
        {
            Logger.Info($"Scraper started: {Assembly.GetEntryAssembly().Location}");
            if (!LoadSettings())
            {
                return;
            }

            var scrapers = new List<Scraper>()
            {
                new LostFilmScraper(GetLastStoredEpisodeId("lostfilm")),
                new NewStudioScraper(GetLastStoredEpisodeId("newstudio"))
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

        private static bool LoadSettings()
        {
            bool result = true;
            try
            {
                updateInterval = int.Parse(ConfigurationManager.AppSettings["UpdateInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading update interval");
                result = false;
            }

            return result;
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
                Thread.Sleep(TimeSpan.FromMinutes(updateInterval));
            }
        }

        private static void UpdateEpisodes(List<Show> shows)
        {
            using (var db = new AppDbContext())
            {
                foreach (var show in shows)
                {
                    var dbShow = db.GetShowByTitle(show.SiteType, show.Title);
                    if (dbShow != null)
                    {
                        dbShow.Episodes.AddRange(show.Episodes);
                    }
                    else
                    {
                        db.Shows.Add(show);
                    }

                    Logger.Info($"{show.Title} - {string.Join(", ", show.Episodes.Select(e => e.Title))}");
                }

                db.SaveChanges();
            }
        }

        private static void UpdateShows(Scraper scraper)
        {
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

                foreach (var show in showsList)
                {
                    var dbShow = db.SiteTypes.First(st => st.Id == scraper.ShowsSiteType.Id).Shows.FirstOrDefault(s => s.SiteId == show.SiteId);
                    if (dbShow != null)
                    {
                        dbShow.OriginalTitle = show.OriginalTitle;
                        dbShow.Description = show.Description ?? dbShow.Description;
                        dbShow.SiteId = show.SiteId;
                    }
                    else
                    {
                        db.Shows.Add(show);
                    }
                }

                db.SaveChanges();
            }
        }

        private static int GetLastStoredEpisodeId(string siteTypeName)
        {
            using (var db = new AppDbContext())
            {
                return db.Episodes?.Where(e => e.Show.SiteType.Name == siteTypeName).Max(e => (int?)e.SiteId) ?? LastStoredEpisodesId[siteTypeName];
            }
        }
    }
}
