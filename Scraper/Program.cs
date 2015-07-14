using System;
using System.Collections.Generic;
using System.Linq;
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
            }
        }
    }
}
