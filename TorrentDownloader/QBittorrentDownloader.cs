using System;
using System.Collections.Generic;
using System.Net.Http;
using NLog;

namespace TorrentDownloader
{
    public class QBittorrentDownloader : ITorrentDownloader
    {
        private const string SuccessAuthResponse = "Ok.";
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public bool Download(Uri torrent, Uri torrenWebUiUri, string password)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{torrenWebUiUri}login");
                var formContent = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("username", "admin"),
                    new KeyValuePair<string, string>("password", password)
                };
                request.Content = new FormUrlEncodedContent(formContent);
                var responseContent = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                if (!responseContent.Equals(SuccessAuthResponse))
                {
                    _logger.Error($"{GetType().Name}: Invalid login or password");
                    return false;
                }
                request = new HttpRequestMessage(HttpMethod.Post, $"{torrenWebUiUri}command/download");
                formContent = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("urls", torrent.AbsoluteUri)
                };
                request.Content = new FormUrlEncodedContent(formContent);
                responseContent = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            }
            return true;
        }
    }
}