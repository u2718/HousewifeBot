using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DAL;
using HtmlAgilityPack;

namespace Scraper
{
    internal abstract class Scraper : IDisposable
    {
        private const int RetryCount = 3;
        private readonly WebClient client = new WebClient();

        protected Scraper(long lastStoredEpisodeId)
        {
            LastStoredEpisodeId = lastStoredEpisodeId;
        }

        public string SiteTitle { get; protected set; }

        public SiteType ShowsSiteType { get; protected set; }

        protected long LastStoredEpisodeId { get; private set; }

        protected abstract string Url { get; }

        protected abstract string ShowsListUrl { get; }

        protected Encoding SiteEncoding { get; set; }

        public List<Show> Load()
        {
            var showDictionary = new Dictionary<string, Show>();
            Dictionary<string, Show> pageShows;
            int pageNumber = 0;
            do
            {
                pageShows = LoadPage(GetPageUrlByNumber(pageNumber));
                foreach (var show in pageShows)
                {
                    if (showDictionary.ContainsKey(show.Key))
                    {
                        // Remove duplicates from series list
                        var seriesList = show.Value.Episodes.Except(showDictionary[show.Key].Episodes);
                        showDictionary[show.Key].Episodes.AddRange(seriesList);
                    }
                    else
                    {
                        showDictionary.Add(show.Key, show.Value);
                    }
                }

                pageNumber++;
            }
            while (pageShows.Count != 0);

            var result = showDictionary.Values.ToList();
            UpdateLastLoadedEpisodeId(result);
            return result;
        }

        public abstract Task<List<Show>> LoadShows();

        public void Dispose()
        {
            client.Dispose();
        }

        protected async Task<HtmlDocument> DownloadDocument(string url)
        {
            string html = string.Empty;
            for (int i = 0; i <= RetryCount; i++)
            {
                try
                {
                    html = SiteEncoding.GetString(await client.DownloadDataTaskAsync(url));
                    break;
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, $"An error occurred while downloading page: {url}");
                    if (i == RetryCount)
                    {
                        throw;
                    }

                    Thread.Sleep(1000);
                }
            }

            var doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception e)
            {
                Program.Logger.Error(e, "An error occurred while creating HtmlDocument");
                throw;
            }

            return doc;
        }

        protected abstract Dictionary<string, Show> LoadPage(string url);

        protected abstract string GetPageUrlByNumber(int pageNumber);

        private void UpdateLastLoadedEpisodeId(List<Show> result)
        {
            if (result.Count != 0)
            {
                LastStoredEpisodeId = result.Aggregate(
                    new List<Episode>(),
                    (list, show) => { list.AddRange(show.Episodes); return list; })
                    .Max(s => s.SiteId);
            }
        }
    }
}