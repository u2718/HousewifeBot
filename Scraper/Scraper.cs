using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DAL;

namespace Scraper
{
    abstract class Scraper
    {
        protected long LastId;
        protected WebClient Client = new WebClient();
        protected string Url;
        protected string ShowsListUrl;

        public int RetryCount { get; private set; }

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
                }
                pageNumber++;
            } while (!stop);

            List<Show> result = showDictionary.Select(s => s.Value).ToList();
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

        public abstract List<Tuple<string, string>> LoadShows();

        protected abstract bool LoadPage(string url, out Dictionary<string, Show> shows);
        protected abstract string GetPageUrlByNumber(int pageNumber);
    }
}
