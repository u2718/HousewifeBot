using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace TorrentDownloader
{
    public class UTorrentDownloader : ITorrentDownloader
    {
        private const string ErrorResultString = "\r\ninvalid request";
        public bool Download(Uri torrent, Uri torrenWebUiUri, string password)
        {
            CookieContainer cc = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("admin", password),
                CookieContainer = cc,
                UseCookies = true
            };
            string result;

            using (HttpClient client = new HttpClient(handler))
            {
                string tokenHtml;
                try
                {
                    tokenHtml = client.GetAsync(torrenWebUiUri + $"token.html?t={CurrentUnixTime()}").Result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    return false;
                }
                string token = Regex.Match(tokenHtml, @"'>(.+?)<\/").Groups[1].Value;

                try
                {
                    result = client.GetAsync(torrenWebUiUri + $"?token={token}&action=add-url&s={Uri.EscapeDataString(torrent.ToString())}&t={CurrentUnixTime()}")
                        .Result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            return result != ErrorResultString;
        }

        private static ulong CurrentUnixTime()
        {
            return (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }

}