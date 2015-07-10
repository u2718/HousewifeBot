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

        protected override bool LoadPage(string url, out Dictionary<string, Show> shows)
        {
            Dictionary<string, Show> result = new Dictionary<string, Show>();

            string html = String.Empty;
            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    html = MClient.DownloadString(url);
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
                shows = result;
            }

            return Parse(html, out shows);
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return MUrl + $"?o={pageNumber*15}";
        }

        private bool Parse(string html, out Dictionary<string, Show> result)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            HtmlDocument doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = new Dictionary<string, Show>();
                return false;
            }

            var showTitles = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                ?.Select(s => s?.Attributes["title"]?.Value?.Trim())
                ?.ToArray();

            var seriesTitles = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //span[@class='torrent_title']//b")
                ?.Select(s => s?.InnerText?.Trim())
                ?.ToArray();

            var seriesIds = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //a[@class='a_details']")
                ?.Select(
                    s => s?.Attributes["href"] != null ?
                    _idRegex.Match(s.Attributes["href"].Value).Groups[1].Value :
                    null)
                ?.ToArray();

            if (showTitles == null || seriesTitles == null || seriesIds == null)
            {
                throw new ArgumentException("Invalid web page", nameof(html));
            }

            var dateList = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']")
                ?.First()?.InnerHtml;
            var dates = dateList != null ? _dateRegex.Matches(dateList) : null;

            Dictionary<string, Show> showDictionary = new Dictionary<string, Show>();
            bool stop = false;
            for (int i = 0; i < showTitles.Length; i++)
            {
                int seriesId = -1;
                try
                {
                    seriesId = int.Parse(seriesIds[i]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                if (seriesId == MLastId)
                {
                    stop = true;
                    break;
                }

                DateTime? date = null;
                if (dates != null)
                {
                    DateTime tempDateTime;
                    date = DateTime.TryParse(dates[i].Groups[1].Value, out tempDateTime) ? 
                        tempDateTime : (DateTime?)null;
                }

                if (!showDictionary.ContainsKey(showTitles[i]))
                {
                    showDictionary.Add(showTitles[i], new Show { Title = showTitles[i] });
                }

                showDictionary[showTitles[i]].SeriesList.Add(
                    new Series
                    {
                        SiteId = seriesId,
                        Title = seriesTitles[i],
                        Date = date
                    }
                );
            }

            result = showDictionary;
            return stop;
        }
    }
}
