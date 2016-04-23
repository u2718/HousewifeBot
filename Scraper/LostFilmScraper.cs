using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAL;
using HtmlAgilityPack;

namespace Scraper
{
    class LostFilmScraper : Scraper
    {
        private static readonly Regex DateRegex = new Regex(@"Дата:\s*<b>(\d\d\.\d\d\.\d\d\d\d\s*\d\d:\d\d)<\/b>");
        private static readonly Regex IdRegex = new Regex(@"id=(\d+)");
        private static readonly Regex ShowUrlRegex = new Regex(@"/browse\.php\?cat=(\d+)");
        private static readonly TimeZoneInfo SiteTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        private const string ShowPageUrl = "?cat=";

        public LostFilmScraper(string url, string showsListUrl, long lastId) : base(url, showsListUrl, lastId)
        {
            SiteTitle = "LostFilm.TV";
            SiteTypeName = "lostfilm";
            using (var db = new AppDbContext())
            {
                SiteType = db.GetSiteTypeByName(SiteTypeName);
            }
        }

        public override async Task<List<Show>> LoadShows()
        {
            HtmlDocument doc = await DownloadDocument(ShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='bb']//a[@class='bb_a']");

            Regex ruTitleRegex = new Regex(@"(.*)<br>");
            Regex engTitleRegex = new Regex(@"\((.*)\)");
            List<Show> shows = showNodes.Select(n =>
                new Show()
                {
                    SiteId = int.Parse(ShowUrlRegex.Match(n.Attributes["href"].Value).Groups[1].Value),
                    Title = ruTitleRegex.Match(n.InnerHtml).Groups[1].Value,
                    OriginalTitle = engTitleRegex.Match(n.Element("span").InnerText).Groups[1].Value
                }
                ).ToList();

            using (AppDbContext db = new AppDbContext())
            {
                foreach (var show in db.Shows.Where(s => s.SiteId == 0 || string.IsNullOrEmpty(s.OriginalTitle)))
                {
                    try
                    {
                        show.SiteId = shows.First(s => s.Title == show.Title).SiteId;
                        show.OriginalTitle = shows.First(s => s.Title == show.Title).OriginalTitle;
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Error(e, "An error occurred while updating SiteId or OriginalTitle");
                    }
                }
                db.SaveChanges();
            }

            using (AppDbContext db = new AppDbContext())
            {
                foreach (var show in shows.Except(db.Shows.Where(s => !string.IsNullOrEmpty(s.Description))))
                {
                    try
                    {
                        show.Description = LoadShowDescription(show);
                        shows.First(s => s.SiteId == show.SiteId).Description = show.Description;
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Error(e, "An error occurred while loading show description");
                    }
                }
                db.SaveChanges();
            }
            return shows;
        }

        protected override bool LoadPage(string url, out Dictionary<string, Show> shows)
        {
            return Parse(DownloadDocument(url).Result, out shows);
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return Url + $"?o={pageNumber * 15}";
        }

        private string LoadShowDescription(Show show)
        {
            string u = $"{Url}{ShowPageUrl}{show.SiteId}";
            HtmlDocument doc = DownloadDocument(u).Result;
            string descriptionText = doc.DocumentNode.SelectNodes("//div[@id='MainDiv']//div[@id='Onwrapper']//div[@class='mid']//div//h1").First().ParentNode.InnerText.Trim();
            if (string.IsNullOrEmpty(descriptionText))
            {
                return string.Empty;
            }

            List<string> descriptionParts =
                descriptionText.Replace("\r", string.Empty)
                    .Replace("\t", string.Empty)
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            return descriptionParts
                .GetRange(1, descriptionParts.Count - 1)
                .Aggregate(
                    string.Empty,
                    ((s, s1) => s + s1 + "\n")
                );
        }

        private bool Parse(HtmlDocument document, out Dictionary<string, Show> result)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var showTitles = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                ?.Select(s => s?.Attributes["title"]?.Value?.Trim())
                .ToArray();

            var seriesTitles = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//span[@class='torrent_title']//b")
                ?.Select(s => s?.InnerText?.Trim())
                .ToArray();

            var episodesIds = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a[@class='a_details']")
                ?.Select(
                    s => s?.Attributes["href"] != null ?
                    IdRegex.Match(s.Attributes["href"].Value).Groups[1].Value :
                    null)
                .ToArray();

            if (showTitles == null || seriesTitles == null || episodesIds == null)
            {
                throw new ArgumentException("Invalid web page", nameof(document));
            }

            var dateList = document.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']")
                ?.First()?.InnerHtml;
            var dates = dateList != null ? DateRegex.Matches(dateList) : null;

            Dictionary<string, Show> showDictionary = new Dictionary<string, Show>();
            bool stop = false;
            for (int i = 0; i < showTitles.Length; i++)
            {
                int episodeId;
                try
                {
                    episodeId = int.Parse(episodesIds[i]);
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, $"An error occurred while converting EpisodeId: {episodesIds[i]}");
                    continue;
                }

                if (episodeId <= LastId)
                {
                    stop = true;
                    break;
                }

                DateTimeOffset? date = null;
                if (dates != null)
                {
                    DateTime tempDateTime;
                    if (DateTime.TryParse(dates[i].Groups[1].Value, out tempDateTime))
                    {
                        date = new DateTimeOffset(tempDateTime, SiteTimeZoneInfo.BaseUtcOffset);
                    }
                }

                if (!showDictionary.ContainsKey(showTitles[i]))
                {
                    showDictionary.Add(showTitles[i], new Show { Title = showTitles[i] });
                }

                showDictionary[showTitles[i]].Episodes.Add(
                    new Episode
                    {
                        SiteId = episodeId,
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
