using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            Scraper s = new LostFilmScraper(@"https://www.lostfilm.tv/browse.php", 14468);
            List<Show> shows = s.Load();

            foreach (var show in shows)
            {
                Console.WriteLine(show.Title);
                foreach (var series in show.SeriesList)
                {
                    Console.WriteLine('\t' + series.Title);
                }
            }
            Console.ReadKey();
        }
    }
}
