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

        public LostFilmScraper(string url, string showsListUrl, long lastId) : base(url, showsListUrl, lastId)
        {

        }

        public override List<Tuple<string, string>> LoadShows()
        {
            HtmlDocument doc = DownloadDocument(MShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='bb']//a[@class='bb_a']");

            Regex ruTitleRegex = new Regex(@"(.*)<br>");
            Regex engTitleRegex = new Regex(@"\((.*)\)");
            return showNodes.Select(n =>
                new Tuple<string, string>
                    (
                    ruTitleRegex.Match(n.InnerHtml).Groups[1].Value,
                    engTitleRegex.Match(n.Element("span").InnerText).Groups[1].Value
                    )
                ).ToList();
        }

        protected override bool LoadPage(string url, out Dictionary<string, Show> shows)
        {
            return Parse(DownloadDocument(url), out shows);
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return MUrl + $"?o={pageNumber*15}";
        }

        private HtmlDocument DownloadDocument(string url)
        {
            string html = string.Empty;
            for (int i = 0; i <= RetryCount; i++)
            {
                try
                {
                    html = MClient.DownloadString(url);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    if (i == RetryCount)
                    {
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }

            HtmlDocument doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            return doc;
        }

        private bool Parse(HtmlDocument document, out Dictionary<string, Show> result)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var showTitles = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                ?.Select(s => s?.Attributes["title"]?.Value?.Trim())
                ?.ToArray();

            var seriesTitles = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //span[@class='torrent_title']//b")
                ?.Select(s => s?.InnerText?.Trim())
                ?.ToArray();

            var seriesIds = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']
                //a[@class='a_details']")
                ?.Select(
                    s => s?.Attributes["href"] != null ?
                    _idRegex.Match(s.Attributes["href"].Value).Groups[1].Value :
                    null)
                ?.ToArray();

            if (showTitles == null || seriesTitles == null || seriesIds == null)
            {
                throw new ArgumentException("Invalid web page", nameof(document));
            }

            var dateList = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']")
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
