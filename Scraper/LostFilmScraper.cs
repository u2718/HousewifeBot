using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAL;
using HtmlAgilityPack;

namespace Scraper
{
    internal class LostFilmScraper : Scraper
    {
        private const string ShowPageUrl = "?cat=";

        private static readonly Regex DateRegex = new Regex(@"Дата:\s*<b>(\d\d\.\d\d\.\d\d\d\d\s*\d\d:\d\d)<\/b>");
        private static readonly Regex IdRegex = new Regex(@"id=(\d+)");
        private static readonly Regex EpisodeNumberRegex = new Regex(@"s=(\d+).*?&e=(\d+)");
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
                    Title = WebUtility.HtmlDecode(rusTitleRegex.Match(n.InnerHtml).Groups[1].Value),
                    OriginalTitle = WebUtility.HtmlDecode(engTitleRegex.Match(n.Element("span").InnerText).Groups[1].Value),
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

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return Url + $"?o={pageNumber * 15}";
        }

        protected override Dictionary<string, Show> LoadPage(string url)
        {
            var doc = DownloadDocument(url).Result;
            string[] showTitles;
            string[] episodesTitles;
            string[] episodesIds;
            Tuple<int, int>[] episodesNumbers;
            bool success = GetEpiodesData(doc, out showTitles, out episodesTitles, out episodesIds, out episodesNumbers);
            if (!success)
            {
                throw new ArgumentException("Invalid web page", nameof(doc));
            }

            var dateList = doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']").First()?.InnerHtml;
            var dates = dateList != null ? DateRegex.Matches(dateList).Cast<Match>().Select(m => m.Groups[1].Value).ToArray() : null;
            var showDictionary = new Dictionary<string, Show>();
            for (int i = 0; i < showTitles.Length; i++)
            {
                var episode = CreateEpisode(episodesIds[i], episodesTitles[i], dates?[i], episodesNumbers[i]);
                if (episode == null)
                {
                    break;
                }

                if (!showDictionary.ContainsKey(showTitles[i]))
                {
                    showDictionary.Add(showTitles[i], new Show { Title = showTitles[i], SiteTypeId = ShowsSiteType.Id });
                }

                showDictionary[showTitles[i]].Episodes.Add(episode);
            }

            return showDictionary;
        }

        private static bool GetEpiodesData(HtmlDocument doc, out string[] showTitles, out string[] episodesTitles, out string[] episodesIds, out Tuple<int, int>[] episodesNumbers)
        {
            showTitles =
                doc.DocumentNode.SelectNodes(@"//div[@class='mid']//div[@class='content_body']//a//img")
                    ?.Select(s => s?.Attributes["title"]?.Value?.Trim())
                    .ToArray();
            episodesTitles =
                doc.DocumentNode.SelectNodes("//div[@class='mid']//div[@class='content_body']//span[@class='torrent_title']//b")
                    ?.Select(s => s?.InnerText?.Trim())
                    .ToArray();
            episodesIds = doc.DocumentNode.SelectNodes("//div[@class='mid']//div[@class='content_body']//a[@class='a_details']")
                ?.Select(
                    s => s?.Attributes["href"] != null
                        ? IdRegex.Match(s.Attributes["href"].Value).Groups[1].Value
                        : null)
                .ToArray();
            episodesNumbers =
                doc.DocumentNode.SelectNodes("//div[@class='mid']//div[@class='content_body']//a[@class='a_discuss']")
                    ?.Select(
                        s => s?.Attributes["href"] != null
                            ? Tuple.Create(
                                int.Parse(EpisodeNumberRegex.Match(s.Attributes["href"].Value).Groups[1].Value),
                                int.Parse(EpisodeNumberRegex.Match(s.Attributes["href"].Value).Groups[2].Value))
                            : null)
                    .ToArray();
            return !(showTitles == null || episodesTitles == null || episodesIds == null || episodesNumbers == null);
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

            return WebUtility.HtmlDecode(descriptionParts.GetRange(1, descriptionParts.Count - 1).Aggregate(string.Empty, (s, s1) => s + s1 + "\n"));
        }

        private Episode CreateEpisode(string episodeId, string title, string date, Tuple<int, int> episodeNumber)
        {
            var episode = new Episode();
            episode.SiteId = int.Parse(episodeId);
            if (episode.SiteId <= LastStoredEpisodeId)
            {
                return null;
            }

            episode.SeasonNumber = episodeNumber.Item1;
            episode.EpisodeNumber = episodeNumber.Item2 != 99 ? episodeNumber.Item2 : 0;
            episode.Title = WebUtility.HtmlDecode(title);
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
