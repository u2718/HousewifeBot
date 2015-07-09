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
                string title = showTitle[i].Attributes["title"].Value.Trim();
                if (result.Count(s => s.Title == title) == 0)
                {
                    result.Add(new Show
                    {
                        Title = title
                    });
                }

                result.First(s => s.Title == title).SeriesList.Add(
                    new Series { Title = seriesTitle[i].InnerText.Trim() }
                );
            }

            return result;
        }
    }
}
