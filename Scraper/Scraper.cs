using System.Collections.Generic;
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

        public abstract List<Show> Load();
    }
}
