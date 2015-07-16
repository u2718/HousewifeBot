using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DAL;

namespace Scraper
{
    class Program
    {
        static void Main()
        {
            using (var db = new AppDbContext())
            {
                int lastId = db.Series?.OrderByDescending(s => s.SiteId).FirstOrDefault()?.SiteId ?? 14468;

                Scraper scraper = new LostFilmScraper(@"https://www.lostfilm.tv/browse.php",
                    @"http://www.lostfilm.tv/serials.php", lastId);

                foreach (var show in scraper.LoadShows())
                {
                    if (db.Shows.Any(s => s.Title == show.Item1))
                    {
                        db.Shows.First(s => s.Title == show.Item1).OriginalTitle = show.Item2;
                    }
                    else
                    {
                        db.Shows.Add(new Show
                        {
                            Title = show.Item1,
                            OriginalTitle = show.Item2
                        });
                    }
                }
                db.SaveChanges();
                while (true)
                {
                    List<Show> shows = scraper.Load();

                    foreach (var show in shows)
                    {
                        if (db.Shows.Any(s => s.Title == show.Title))
                        {
                            db.Shows.First(s => s.Title == show.Title)
                                .SeriesList.AddRange(show.SeriesList);
                        }
                        else
                        {
                            db.Shows.Add(show);
                        }

                        Console.WriteLine(show.Title);
                        foreach (var series in show.SeriesList)
                        {
                            Console.WriteLine('\t' + series.Title);
                        }
                    }
                    db.SaveChanges();
                    Thread.Sleep(TimeSpan.FromMinutes(10));
                }
            }
        }
    }
}
