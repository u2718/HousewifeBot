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
    internal class NewStudioScraper : Scraper
    {
        private const string ShowPageUrl = @"http://newstudio.tv/viewforum.php?f={0}";
        private const string SiteUrl = @"http://newstudio.tv";
        private static readonly Regex EpisodeNumberRegex = new Regex(@".+?\(Сезон\s*(\d+),\s* Серия\s*(\d+)\)");
        private static readonly Regex SeasonNumberRegex = new Regex(@"Сезон\s*(\d+)");
        private static readonly Regex IdRegex = new Regex(@"f=(\d+)");
        private static readonly Regex OriginalTitleRegex = new Regex(@"\/(.+)\(\d{4}\)");
        private static readonly Regex EpisodeSiteIdRegex = new Regex(@"\?t=(\d+)");
        private static readonly Regex EpisodeRussianTitleRegex = new Regex(@"Русскоязычное название:\s*</span>\s*(.+?)<span");
        private static readonly Regex EpisodeTitleRegex = new Regex(@"Название серии:\s*</span>\s*(.+?)<span");
        private static readonly Regex SeasonTitleRegex = new Regex(@"Сезон:\s*</span>\s*(.+?)<span");
        private static readonly Regex EpisodeDateRegex = new Regex(@"(\d{2}-.+?-\d{2}).+?(\d{2}:\d{2})");
        private static readonly TimeZoneInfo SiteTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        public NewStudioScraper(long lastStoredEpisodeId) : base(lastStoredEpisodeId)
        {
            SiteTitle = "NewStudio.TV";
            SiteEncoding = Encoding.UTF8;
            using (var db = new AppDbContext())
            {
                ShowsSiteType = db.GetSiteTypeByName("newstudio");
            }
        }

        protected override string Url { get; } = @"http://newstudio.tv/tracker.php";

        protected override string ShowsListUrl { get; } = @"http://newstudio.tv/";

        public override async Task<List<Show>> LoadShows()
        {
            var doc = await DownloadDocument(ShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@id='serialist']//li//a");
            var shows = showNodes.Select(n => new Show()
            {
                SiteId = int.Parse(IdRegex.Match(n.Attributes["href"].Value).Groups[1].Value),
                Title = WebUtility.HtmlDecode(n.InnerText),
                SiteTypeId = ShowsSiteType.Id
            }).ToList();
            using (var db = new AppDbContext())
            {
                foreach (var show in shows)
                {
                    var dbShow = db.Shows.FirstOrDefault(s => s.Title == show.Title);
                    show.OriginalTitle = dbShow?.OriginalTitle ?? await GetOriginalTitle(show.SiteId);
                }
            }

            return shows;
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return Url + $"?start={pageNumber * 50}";
        }

        protected override Dictionary<string, Show> LoadPage(string url)
        {
            var doc = DownloadDocument(url).Result;
            var episodeNodes = doc.DocumentNode.SelectNodes(@"//table[@class='table well well-small']//a[@class='genmed']");
            var dateNodes = doc.DocumentNode.SelectNodes(@"//table[@class='table well well-small']//td[@class='row4 small']");
            var episodes = new HashSet<Tuple<string, int, int>>();
            var showDictionary = new Dictionary<string, Show>();
            for (int i = 0; i < episodeNodes.Count; i++)
            {
                var currentNode = episodeNodes[i];
                var detailsUrl = SiteUrl + currentNode.Attributes["href"].Value;
                var showTitle = GetShowTitle(currentNode);

                if (IsAlreadyAdded(currentNode, showTitle, ref episodes))
                {
                    continue;
                }

                var episode = CreateEpisode(currentNode, detailsUrl, dateNodes[i]);
                if (episode == null)
                {
                    break;
                }

                if (!showDictionary.ContainsKey(showTitle))
                {
                    showDictionary.Add(showTitle, new Show { Title = showTitle, SiteTypeId = ShowsSiteType.Id });
                }

                showDictionary[showTitle].Episodes.Add(episode);
            }

            return showDictionary;
        }

        private static bool IsAlreadyAdded(HtmlNode node, string showTitle, ref HashSet<Tuple<string, int, int>> episodesSet)
        {
            int seasonNumber = GetSeasonNumber(node);
            int episodeNumber = GetEpisodeNumber(node);
            if (episodesSet.Contains(Tuple.Create(showTitle, seasonNumber, episodeNumber)))
            {
                return true;
            }

            episodesSet.Add(Tuple.Create(showTitle, seasonNumber, episodeNumber));
            return false;
        }

        private static string GetShowTitle(HtmlNode currentNode)
        {
            return WebUtility.HtmlDecode(OriginalTitleRegex.Match(currentNode.InnerText).Groups[1].Value.Trim());
        }

        private static int GetSeasonNumber(HtmlNode node)
        {
            return int.Parse(SeasonNumberRegex.Match(node.InnerText).Groups[1].Value);
        }

        private static int GetEpisodeNumber(HtmlNode node)
        {
            return EpisodeNumberRegex.IsMatch(node.InnerText) ? int.Parse(EpisodeNumberRegex.Match(node.InnerText).Groups[2].Value) : 0;
        }

        private Episode CreateEpisode(HtmlNode node, string detailsUrl, HtmlNode dateNode)
        {
            var episode = new Episode();
            episode.SiteId = int.Parse(EpisodeSiteIdRegex.Match(detailsUrl).Groups[1].Value);
            if (episode.SiteId <= LastStoredEpisodeId)
            {
                return null;
            }

            episode.SeasonNumber = GetSeasonNumber(node);
            episode.EpisodeNumber = GetEpisodeNumber(node);
            episode.Title = GetEpisodeTitle(detailsUrl);
            var dateMatch = EpisodeDateRegex.Match(dateNode.InnerText);
            string date = $"{dateMatch.Groups[1].Value} {dateMatch.Groups[2].Value}";
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

        private async Task<string> GetOriginalTitle(int showId)
        {
            HtmlDocument doc;
            try
            {
                doc = await DownloadDocument(string.Format(ShowPageUrl, showId));
            }
            catch (WebException)
            {
                return string.Empty;
            }

            var node = doc.DocumentNode.SelectSingleNode(@"//div[@class='topic-list']/a/b");
            return node == null ? string.Empty : WebUtility.HtmlDecode(OriginalTitleRegex.Match(node.InnerText).Groups[1].Value.Trim());
        }

        private string GetEpisodeTitle(string url)
        {
            HtmlDocument doc;
            try
            {
                doc = DownloadDocument(url).Result;
            }
            catch (WebException)
            {
                return string.Empty;
            }

            var details = doc.DocumentNode.SelectSingleNode("//div[@class='post_wrap']").InnerHtml;
            string title;
            if (EpisodeRussianTitleRegex.IsMatch(details))
            {
                title = EpisodeRussianTitleRegex.Match(details).Groups[1].Value;
            }
            else if (EpisodeTitleRegex.IsMatch(details))
            {
                title = EpisodeTitleRegex.Match(details).Groups[1].Value;
            }
            else if (EpisodeNumberRegex.IsMatch(details))
            {
                title = $"{EpisodeNumberRegex.Match(details).Groups[2].Value} серия";
            }
            else
            {
                title = $"{SeasonTitleRegex.Match(details).Groups[1].Value} сезон полностью";
            }

            return WebUtility.HtmlDecode(title);
        }
    }
}