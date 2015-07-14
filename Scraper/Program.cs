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
                var a = db.Shows.ToArray();

                Scraper scraper = new LostFilmScraper(@"https://www.lostfilm.tv/browse.php",
                    @"http://www.lostfilm.tv/serials.php", lastId);
                List<Show> shows = scraper.Load();

                db.Shows.AddRange(shows);

                foreach (var show in shows)
                {
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
