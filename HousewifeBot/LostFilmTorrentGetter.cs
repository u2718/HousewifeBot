using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using DAL;
using HtmlAgilityPack;

namespace HousewifeBot
{
    public class LostFilmTorrentGetter : ITorrentGetter
    {
        private const string LoginUrl = @"https://login1.bogi.ru/login.php?referer=https%3A%2F%2Fwww.lostfilm.tv%2F";
        private const string DetailsUrl = @"http://www.lostfilm.tv/details.php?id={0}";
        private const string DownloadsUrl = @"https://www.lostfilm.tv/nrdr.php?c={0}&s={1}&e={2}";
        private readonly char[] CharsToReplace = {' ', '-'};

        private HttpClient _client;

        public List<TorrentDescription> GetEpisodeTorrents(Episode episode, string login, string password)
        {
            _client = Login(login, password);
            string detailsContent = _client.GetAsync(string.Format(DetailsUrl, episode.SiteId)).Result.Content.ReadAsStringAsync().Result;

            Match parametersMatch = Regex.Match(detailsContent, @"ShowAllReleases\(\'(.+?)\',\s*\'(.+?)\',\s*\'(.+?)\'\)");

            if (!parametersMatch.Success)
            {
                throw new Exception();
            }

            IEnumerable<TorrentDescription> torrents = GetTorrents(parametersMatch.Groups[1].Value, parametersMatch.Groups[2].Value, parametersMatch.Groups[3].Value);

            return torrents.ToList();
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

        private IEnumerable<TorrentDescription> GetTorrents(string showId, string seasonNumber, string episodeNumber)
        {
            string downloadsContent = Encoding.GetEncoding(1251).GetString(_client.GetAsync(string.Format(DownloadsUrl, showId, seasonNumber, episodeNumber)).Result.Content.ReadAsByteArrayAsync().Result);
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(downloadsContent);

            List<HtmlNode> torrentNodes = document.DocumentNode
                .SelectNodes("//div//table//tr//td//span")
                .Where(n => n.Element("div") != null).ToList();
            List<TorrentDescription> torrents = new List<TorrentDescription>();
            foreach (var node in torrentNodes)
            {
                string description = node.InnerText;
                description = description.Substring(0, description.IndexOf('\n'));
                string uri = node.Element("div").Element("nobr").Element("a").InnerHtml;

                Match sizeMatch = Regex.Match(description, @"Размер: (.+)\.");
                Match qualityMatch = Regex.Match(description, @"Видео: (.+?)\.");

                TorrentDescription torrentDescription = new TorrentDescription()
                {
                    TorrentUri = new Uri(uri),
                    Description = description,
                    Size = sizeMatch.Success ? sizeMatch.Groups[1].Value : string.Empty,
                    Quality = qualityMatch.Success ? qualityMatch.Groups[1].Value : string.Empty
                };

                foreach (char c in CharsToReplace)
                {
                    torrentDescription.Quality = torrentDescription.Quality.Replace(c, '_');
                }

                torrents.Add(torrentDescription);
            }

            return torrents;
        }


    }
}