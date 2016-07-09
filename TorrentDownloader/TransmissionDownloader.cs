using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace TorrentDownloader
{
    public class TransmissionDownloader : ITorrentDownloader
    {
        public bool Download(Uri torrent, Uri torrenWebUiUri, string password)
        {
            var handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("admin", password),
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            using (HttpClient client = new HttpClient(handler))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(torrenWebUiUri, "rpc"));
                var request = new
                {
                    method = "torrent-add",
                    arguments = new
                    {
                        paused = false,
                        filename = torrent.AbsoluteUri
                    }
                };

                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request));
                dynamic response = JsonConvert.DeserializeObject(client.SendAsync(requestMessage).Result.Content.ReadAsStringAsync().Result);
                return string.Equals(response.result.ToString(), "success", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}