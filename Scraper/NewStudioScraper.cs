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
    class NewStudioScraper : Scraper
    {
        private const string SiteUrl = @"http://newstudio.tv";
        private const string ShowPageUrl = @"http://newstudio.tv/viewforum.php?f={0}";
        private static readonly Regex IdRegex = new Regex(@"f=(\d+)");
        private static readonly Regex OriginalTitleRegex = new Regex(@"\/(.+)\(\d{4}\)");
        private static readonly Regex EpisodeNumberRegex = new Regex(@".+?\(.+?,\s* Серия\s*(\d+)\)");

        public NewStudioScraper(string url, string showsListUrl, long lastId) : base(url, showsListUrl, lastId)
        {
            SiteTitle = "NewStudio.TV";
            SiteTypeName = "newstudio";
            SiteEncoding = Encoding.UTF8;
            using (var db = new AppDbContext())
            {
                SiteType = db.GetSiteTypeByName(SiteTypeName);
            }
        }

        public override async Task<List<Show>> LoadShows()
        {
            var doc = await DownloadDocument(ShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@id='serialist']//li//a");
            var shows = showNodes.Select(n => new Show()
            {
                SiteId = int.Parse(IdRegex.Match(n.Attributes["href"].Value).Groups[1].Value),
                Title = n.InnerText,
                SiteTypeId = this.SiteType.Id
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

        private async Task<string> GetOriginalTitle(int showId)
        {
            HtmlDocument doc;
            try
            {
                doc = await DownloadDocument(String.Format(ShowPageUrl, showId));
            }
            catch (WebException)
            {
                return String.Empty;
            }
            var node = doc.DocumentNode.SelectSingleNode(@"//div[@class='topic-list']/a/b");
            return node == null ? string.Empty : OriginalTitleRegex.Match(node.InnerText).Groups[1].Value.Trim();
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            return Url + $"?start={pageNumber*50}";
        }

        protected override bool LoadPage(string url, out Dictionary<string, Show> shows)
        {
            var doc = DownloadDocument(url).Result;
            var episodeNodes = doc.DocumentNode.SelectNodes(@"//table[@class='table well well-small']//a[@class='genmed']");
            Dictionary<string, int> episodes = new Dictionary<string, int>();
            var detailsUrls = new List<string>();
            foreach (var node in episodeNodes)
            {
                var detailsUrl = SiteUrl + node.Attributes["href"].Value;
                var title = OriginalTitleRegex.Match(node.InnerText).Groups[1].Value;
                var episodeNumber = int.Parse(EpisodeNumberRegex.Match(node.InnerText).Groups[1].Value);
                if (episodes.ContainsKey(title) && episodes[title] == episodeNumber)
                    continue;
                episodes.Add(title, episodeNumber);
                detailsUrls.Add(detailsUrl);
            }

            shows = null;
            return false;
        }

        private Episode LoadEpisode(string url)
        {
            return null;
        }
    }
}