using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using NLog;

namespace TorrentDownloader
{
    public class UTorrentDownloader : ITorrentDownloader
    {
        private const string ErrorResultString = "\r\ninvalid request";
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

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
                    logger.Debug($"{GetType().Name}: Retrieving token page");
                    tokenHtml = client.GetAsync(torrenWebUiUri + $"token.html?t={CurrentUnixTime()}").Result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    logger.Error(e, $"{GetType().Name}: An error occurred while retrieving token page");
                    return false;
                }

                logger.Debug($"{GetType().Name}: Parsing token page");
                string token;
                try
                {
                    token = Regex.Match(tokenHtml, @"'>(.+?)<\/").Groups[1].Value;
                }
                catch (Exception e)
                {
                    logger.Error(e, $"{GetType().Name}: An error occurred while parsing token page");
                    return false;
                }

                logger.Debug($"{GetType().Name}: Adding torrent using WebUi");
                try
                {
                    result = client.GetAsync(torrenWebUiUri + $"?token={token}&action=add-url&s={Uri.EscapeDataString(torrent.ToString())}&t={CurrentUnixTime()}")
                        .Result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    logger.Error(e, $"{GetType().Name}: An error occurred while adding torrent");
                    return false;
                }
            }

            logger.Debug($"{GetType().Name}: result: {result}");
            return result != ErrorResultString;
        }

        private static ulong CurrentUnixTime()
        {
            return (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }

}