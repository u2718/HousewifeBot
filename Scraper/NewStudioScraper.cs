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
        private static readonly Regex EpisodeNumberRegex = new Regex(@".+?\(.+?,\s* Серия\s*(\d+)\)");
        private static readonly Regex IdRegex = new Regex(@"f=(\d+)");
        private static readonly Regex OriginalTitleRegex = new Regex(@"\/(.+)\(\d{4}\)");

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
                Title = n.InnerText,
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
            var episodes = new HashSet<Tuple<string, int>>();
            var detailsUrls = new List<string>();
            foreach (var node in episodeNodes)
            {
                var detailsUrl = SiteUrl + node.Attributes["href"].Value;
                var title = OriginalTitleRegex.Match(node.InnerText).Groups[1].Value.Trim();
                var episodeNumber = int.Parse(EpisodeNumberRegex.Match(node.InnerText).Groups[1].Value);
                if (episodes.Contains(Tuple.Create(title, episodeNumber)))
                {
                    continue;
                }

                episodes.Add(Tuple.Create(title, episodeNumber));
                detailsUrls.Add(detailsUrl);
            }

            return null;
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
            return node == null ? string.Empty : OriginalTitleRegex.Match(node.InnerText).Groups[1].Value.Trim();
        }

        private Episode LoadEpisode(string url)
        {
            return null;
        }
    }
}