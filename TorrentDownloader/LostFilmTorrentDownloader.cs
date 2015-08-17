using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using DAL;

namespace TorrentDownloader
{
    public class LostFilmTorrentDownloader : ITorrentDownloader
    {
        const string LoginUrl = @"https://login1.bogi.ru/login.php?referer=https%3A%2F%2Fwww.lostfilm.tv%2F";
      
        public List<Uri> GetEpisodeTorrents(Episode episode, string login, string password)
        {
            HttpClient client = Login(login, password);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs authentication to lostfilm.tv
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns>HttpClient with required cookies</returns>
        private HttpClient Login(string login, string password)
        {
            CookieContainer cc = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cc,
                UseCookies = true,
                UseProxy = false
            };

            HttpClient client = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, LoginUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"login", login},
                    {"password", password},
                    {"module", "1"},
                    {"target", "http://www.lostfilm.tv/"},
                    {"repage", "user"},
                    {"act", "login"}
                })
            };

            string response = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;

            string url = Regex.Match(response, @"action=""(.+?)""").Groups[1].Value;

            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("An error occurred while retrieving URL. Login/password is probably incorrect.");
            }

            Regex inputRegex = new Regex(@"<input.*name=""(.+?)"".*value=""(.+?)""");
            List<KeyValuePair<string, string>> formDictionary = response.Split('\n')
                .Where(s => inputRegex.IsMatch(s))
                .Select(s => new KeyValuePair<string, string>(inputRegex.Match(s).Groups[1].Value, inputRegex.Match(s).Groups[2].Value))
                .ToList();

            FormUrlEncodedContent content = new FormUrlEncodedContent(formDictionary);

            request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            client.SendAsync(request).Wait();
            return client;
        }
    }
}