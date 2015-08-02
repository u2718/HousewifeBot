using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DAL;
using NLog;
using System.Configuration;

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

            if (!LoadSettings())
            {
                return;
            }

            using (var db = new AppDbContext())
            {
                int lastId;
                Logger.Debug("Retrieving last episode Id from database");
                try
                {
                    lastId = db.Episodes?.OrderByDescending(s => s.SiteId).FirstOrDefault()?.SiteId ?? 14468;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "An error occurred while retrieving last episode Id");
                    return;
                }


                Scraper scraper = new LostFilmScraper(@"https://www.lostfilm.tv/browse.php",
                    @"http://www.lostfilm.tv/serials.php", lastId);

                List<Tuple<string, string>> showsTuples;
                Logger.Debug($"Retrieving shows from {scraper.SiteTitle}");
                try
                {
                    showsTuples = scraper.LoadShows();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"An error occurred while retrieving shows from {scraper.SiteTitle}");
                    return;
                }

                int newShowsCount = 0;
                try
                {
                    foreach (var showTuple in showsTuples)
                    {
                        if (db.Shows.Any(s => s.Title == showTuple.Item2))
                        {
                            db.Shows.First(s => s.Title == showTuple.Item2).OriginalTitle = showTuple.Item1;
                        }
                        else
                        {
                            db.Shows.Add(new Show
                            {
                                Title = showTuple.Item2,
                                OriginalTitle = showTuple.Item1
                            });
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

                    newShowsCount = 0;
                    int newEpisodesCount = 0;
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
                        
                        Logger.Info($"{show.OriginalTitle} - {string.Join(", ", show.Episodes)}");
                    }

                    if (newShowsCount > 0)
                    {
                        Logger.Info($"{newShowsCount} new {(newShowsCount == 1 ? "show" : "shows")} added");
                    }
                    if (newEpisodesCount > 0)
                    {
                        Logger.Info($"{newEpisodesCount} new {(newEpisodesCount == 1 ? "episode" : "episodes")} added");
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

                    Thread.Sleep(TimeSpan.FromMinutes(_updateInterval));
                }
            }
        }
    }
}
