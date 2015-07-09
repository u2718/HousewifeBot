using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DAL;
using HtmlAgilityPack;

namespace Scraper
{
    class LostFilmScraper : Scraper
    {
        private readonly Regex _dateRegex = new Regex(@"Дата:\s*<b>(\d\d\.\d\d\.\d\d\d\d\s*\d\d:\d\d)<\/b>");
        private readonly Regex _idRegex = new Regex(@"id=(\d+)");

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
            
            result = Parse(html).Select(i => i.Value).ToList(); 

            return result;
        }


        private Dictionary<string, Show> Parse(string html)
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

            var showTitles = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                .Select(s => s?.Attributes["title"]?.Value?.Trim())
                .ToArray();

            var seriesTitles = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //span[@class='torrent_title']//b")
                .Select(s => s?.InnerText?.Trim())
                .ToArray();

            var seriesIds = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //a[@class='a_details']").
                Select(
                    s => s?.Attributes["href"] != null ?
                    _idRegex.Match(s.Attributes["href"].Value).Groups[1].Value :
                    null
                ).ToArray();

            var dates = _dateRegex.Matches(doc.DocumentNode.SelectNodes(@"//div[@class='mid']
                        //div[@class='content_body']").First().InnerHtml);

            Dictionary<string, Show> result = new Dictionary<string, Show>();

            for (int i = 0; i < showTitles.Length; i++)
            {
                if (!result.ContainsKey(showTitles[i]))
                {
                    result.Add(showTitles[i], new Show { Title = showTitles[i] });
                }
                result[showTitles[i]].SeriesList.Add(
                    new Series
                    {
                        SiteId = int.Parse(seriesIds[i]),
                        Title = seriesTitles[i],
                        Date = DateTime.Parse(dates[i].Groups[1].Value)
                    }
                );
            }

            return result;
        }
    }
}
