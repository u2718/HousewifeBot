using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAL;

namespace Scraper
{
    class NewStudioScraper : Scraper
    {
        private const string ShowPageUrl = @"http://newstudio.tv/viewforum.php?f={0}";
        private static readonly Regex IdRegex = new Regex(@"f=(\d+)");
        private static readonly Regex OriginalTitle = new Regex(@"\/(.+)\(\d{4}\)");
        public NewStudioScraper(string url, string showsListUrl, long lastId) : base(url, showsListUrl, lastId)
        {

        }

        public override async Task<List<Show>> LoadShows()
        {
            var doc = await DownloadDocument(ShowsListUrl);
            var showNodes = doc.DocumentNode.SelectNodes(@"//div[@id='serialist']//li//a");
            var shows = showNodes.Select(n => new Show()
            {
                SiteId = int.Parse(IdRegex.Match(n.Attributes["href"].Value).Groups[1].Value),
                Title = n.InnerText
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
            var doc = await DownloadDocument(String.Format(ShowPageUrl, showId));
            var node = doc.DocumentNode.SelectSingleNode(@"//div[@class='topic-list']/a/b");
            return node == null ? string.Empty : OriginalTitle.Match(node.InnerText).Groups[1].Value.Trim();
        }

        protected override string GetPageUrlByNumber(int pageNumber)
        {
            throw new NotImplementedException();
        }

        protected override bool LoadPage(string url, out Dictionary<string, Show> shows)
        {
            throw new NotImplementedException();
        }
    }
}