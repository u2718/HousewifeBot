using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace TorrentDownloader
{
    public class UTorrentDownloader : ITorrentDownloader
    {
        public void Download(Uri torrent, Uri torrenWebUiUri, string password)
        {
            CookieContainer cc = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("admin", password),
                CookieContainer = cc,
                UseCookies = true
            };
            HttpClient client = new HttpClient(handler);

            var tokenHtml = client.GetAsync(torrenWebUiUri + $"token.html?t={CurrentUnixTime()}").Result.Content.ReadAsStringAsync().Result;
            string token = Regex.Match(tokenHtml, @"'>(.+?)<\/").Groups[1].Value;

            client.GetAsync(torrenWebUiUri + $"?token={token}&action=add-url&s={Uri.EscapeDataString(torrent.ToString())}&t={CurrentUnixTime()}").Wait();
        }

        private static ulong CurrentUnixTime()
        {
            return (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }

}