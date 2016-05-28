using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAL;

namespace Scraper
{
    internal class LostFilmScraper : Scraper
    {
        private const string ShowPageUrl = "?cat=";

        private static readonly Regex DateRegex = new Regex(@"Дата:\s*<b>(\d\d\.\d\d\.\d\d\d\d\s*\d\d:\d\d)<\/b>");
        private static readonly Regex IdRegex = new Regex(@"id=(\d+)");
        private static readonly Regex ShowUrlRegex = new Regex(@"/browse\.php\?cat=(\d+)");
        private static readonly TimeZoneInfo SiteTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        public LostFilmScraper(long lastStoredEpisodeId) : base(lastStoredEpisodeId)
        {
            SiteTitle = "LostFilm.TV";
            SiteEncoding = Encoding.GetEncoding(1251);
            using (var db = new AppDbContext())
            {
                ShowsSiteType = db.GetSiteTypeByName("lostfilm");
            }
        }

        protected override string Url { get; } = @"https://www.lostfilm.tv/browse.php";

        protected override string ShowsListUrl { get; } = "http://www.lostfilm.tv/serials.php";

        public override async Task<List<Show>> LoadShows()
        {
            var doc = await DownloadDocument(ShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='bb']//a[@class='bb_a']");

            var rusTitleRegex = new Regex(@"(.*)<br>");
            var engTitleRegex = new Regex(@"\((.*)\)");
            var shows = showNodes.Select(n =>
                new Show()
                {
                    SiteId = int.Parse(ShowUrlRegex.Match(n.Attributes["href"].Value).Groups[1].Value),
                    Title = rusTitleRegex.Match(n.InnerHtml).Groups[1].Value,
                    OriginalTitle = engTitleRegex.Match(n.Element("span").InnerText).Groups[1].Value,
                    SiteTypeId = ShowsSiteType.Id
                })
                .ToList();

            using (var db = new AppDbContext())
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

        protected override Dictionary<string, Show> LoadPage(string url)
        {
            var doc = DownloadDocument(url).Result;
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var showTitles =
                doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                    ?.Select(s => s?.Attributes["title"]?.Value?.Trim())
                    .ToArray();
            var episodesTitles =
                doc.DocumentNode.SelectNodes("//div[@class='mid']//div[@class='content_body']//span[@class='torrent_title']//b")
                    ?.Select(s => s?.InnerText?.Trim())
                    .ToArray();
            var episodesIds =
                doc.DocumentNode.SelectNodes("//div[@class='mid']//div[@class='content_body']//a[@class='a_details']")
                    ?.Select(
                        s => s?.Attributes["href"] != null
                            ? IdRegex.Match(s.Attributes["href"].Value).Groups[1].Value
                            : null)
                    .ToArray();
            if (showTitles == null || episodesTitles == null || episodesIds == null)
            {
                throw new ArgumentException("Invalid web page", nameof(doc));
            }

            var dateList = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']").First()?.InnerHtml;
            var dates = dateList != null ? DateRegex.Matches(dateList).Cast<Match>().Select(m => m.Groups[1].Value).ToArray() : null; // TODO: test for null reference
            var showDictionary = new Dictionary<string, Show>();
            for (int i = 0; i < showTitles.Length; i++)
            {
                var episode = CreateEpisode(episodesIds[i], episodesTitles[i], dates?[i]);
                if (episode == null)
                {
                    break;
                }

                if (!showDictionary.ContainsKey(showTitles[i]))
                {
                    showDictionary.Add(showTitles[i], new Show { Title = showTitles[i], SiteType = ShowsSiteType });
                }

                showDictionary[showTitles[i]].Episodes.Add(episode);
            }

            return showDictionary;
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return Url + $"?o={pageNumber * 15}";
        }

        private string LoadShowDescription(Show show)
        {
            string u = $"{Url}{ShowPageUrl}{show.SiteId}";
            var doc = DownloadDocument(u).Result;
            string descriptionText = doc.DocumentNode.SelectNodes("//div[@id='MainDiv']//div[@id='Onwrapper']//div[@class='mid']//div//h1").First().ParentNode.InnerText.Trim();
            if (string.IsNullOrEmpty(descriptionText))
            {
                return string.Empty;
            }

            var descriptionParts =
                descriptionText.Replace("\r", string.Empty)
                    .Replace("\t", string.Empty)
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            return descriptionParts
                .GetRange(1, descriptionParts.Count - 1)
                .Aggregate(string.Empty, (s, s1) => s + s1 + "\n");
        }

        private Episode CreateEpisode(string episodeId, string title, string date)
        {
            var episode = new Episode();
            episode.Title = title;
            episode.SiteId = int.Parse(episodeId);
            if (episode.SiteId <= LastStoredEpisodeId)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(date))
            {
                DateTime tempDateTime;
                if (DateTime.TryParse(date, out tempDateTime))
                {
                    episode.Date = new DateTimeOffset(tempDateTime, SiteTimeZoneInfo.BaseUtcOffset);
                }
            }

            return episode;
        }
    }
}
