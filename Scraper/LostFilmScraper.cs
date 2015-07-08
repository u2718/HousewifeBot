using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DAL;
using HtmlAgilityPack;

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
                    html = MClient.DownloadString(MUrl);
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

            var showTitle = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                .ToArray();

            var seriesTitle = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //span[@class='torrent_title']//b")
                .ToArray();


            List<Show> result = new List<Show>();
            for (int i = 0; i < showTitle.Length; i++)
            {
                List<Series> seriesList = new List<Series> {new Series {Title = seriesTitle[i].InnerText.Trim()}};

                result.Add(new Show
                {
                    Title = showTitle[i].Attributes["title"].Value.Trim(),
                    SeriesList = new List<Series>(seriesList)
                });
            }

            return result;
        }
    }
}
