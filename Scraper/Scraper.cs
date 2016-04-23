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
    abstract class Scraper
    {
        protected long LastId;
        protected WebClient Client = new WebClient();
        protected string Url;
        protected string ShowsListUrl;

        public int RetryCount { get; private set; }
        public string SiteTitle { get; protected set; }
        public string SiteTypeName  { get; protected set; }
        public SiteType SiteType { get; protected set; }

        protected Scraper(string url, string showsListUrl, long lastId)
        {
            Url = url;
            LastId = lastId;
            RetryCount = 3;
            ShowsListUrl = showsListUrl;
        }

        public List<Show> Load()
        {
            bool stop;
            Dictionary<string, Show> showDictionary = new Dictionary<string, Show>();

            int pageNumber = 0;
            do
            {
                Dictionary<string, Show> shows;
                stop = LoadPage(GetPageUrlByNumber(pageNumber), out shows);
                foreach (var show in shows)
                {
                    if (showDictionary.ContainsKey(show.Key))
                    {
                        //Remove duplicates from series list
                        var seriesList = show.Value.Episodes.Except(showDictionary[show.Key].Episodes);
                        showDictionary[show.Key].Episodes.AddRange(seriesList);
                    }
                    else
                    {
                        showDictionary.Add(show.Key, show.Value);
                    }
                    show.Value.Episodes.ForEach(e => e.SiteType = SiteType);
                }
                pageNumber++;
            } while (!stop);

            List<Show> result = showDictionary.Values.ToList();
            if (result.Count != 0)
            {
                LastId = result.Aggregate(
                    new List<Episode>(),
                    (list, show) =>
                    {
                        list.AddRange(show.Episodes);
                        return list;
                    }
                    ).OrderByDescending(s => s.SiteId).First().SiteId;
            }
            return result;
        }

        protected async Task<HtmlDocument> DownloadDocument(string url)
        {
            string html = string.Empty;
            for (int i = 0; i <= RetryCount; i++)
            {
                try
                {
                    html = Encoding.UTF8.GetString(await Client.DownloadDataTaskAsync(url));
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

            HtmlDocument doc = new HtmlDocument();
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

        public abstract Task<List<Show>> LoadShows();
        protected abstract bool LoadPage(string url, out Dictionary<string, Show> shows);
        protected abstract string GetPageUrlByNumber(int pageNumber);
    }
}
