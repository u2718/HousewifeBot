using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            Scraper s = new LostFilmScraper(@"https://www.lostfilm.tv/browse.php", 0);
            s.Load();

            Console.ReadKey();
        }
    }
}
