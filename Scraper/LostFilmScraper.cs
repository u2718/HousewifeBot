using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;
using HtmlAgilityPack;
using System.Threading;

namespace Scraper
{
    class LostFilmScraper : Scraper
    {
        public LostFilmScraper(string url, long lastId) : base(url, lastId)
        {

        }

        public override List<Show> Load()
        {
            List<Show> result = new List<Show>();

            string html = String.Empty;
            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    html = m_client.DownloadString(m_url);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(1000);
                }
            }

            if (String.IsNullOrEmpty(html))
            {
                return result;
            }

            result = Parse(html);

            return result;
        }


        private List<Show> Parse(string html)
        {
            List<Show> result = new List<Show>();

            HtmlDocument doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            var series = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//span");

            return result;
        }
    }
}
