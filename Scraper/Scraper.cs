using System.Collections.Generic;
using System.Linq;
using System.Net;
using DAL;

namespace Scraper
{
    abstract class Scraper
    {
        protected long MLastId;
        protected WebClient MClient = new WebClient();
        protected string MUrl;

        public int RetryCount { get; private set; }

        protected Scraper(string url, long lastId)
        {
            MUrl = url;
            MLastId = lastId;
            RetryCount = 3;
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
                        var seriesList = show.Value.SeriesList.Except(showDictionary[show.Key].SeriesList);

                        showDictionary[show.Key].SeriesList.AddRange(seriesList);
                    }
                    else
                    {
                        showDictionary.Add(show.Key, show.Value);
                    }
                }
                pageNumber++;
            } while (!stop);

            return showDictionary.Select(s => s.Value).ToList();
        }

        protected abstract bool LoadPage(string url, out Dictionary<string, Show> shows);
        protected abstract string GetPageUrlByNumber(int pageNumber);
    }
}
