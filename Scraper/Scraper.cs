using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;
using System.Net;

namespace Scraper
{
    abstract class Scraper
    {
        protected long m_lastId = 0;
        protected WebClient m_client = new WebClient();
        protected string m_url = String.Empty;

        public int RetryCount { get; private set; }

        public Scraper(string url, long lastId)
        {
            m_url = url;
            m_lastId = lastId;
            RetryCount = 3;
        }

        public abstract List<Show> Load();
    }
}
