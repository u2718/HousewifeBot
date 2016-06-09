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
        private static readonly Regex ParametersRegex = new Regex(@"ShowAllReleases\(\'(.+?)\',\s*\'(.+?)\',\s*\'(.+?)\'\)");
        private static readonly Regex UrlRegex = new Regex(@"action=""(.+?)""");
        private static readonly Regex InputRegex = new Regex(@"<input.*name=""(.+?)"".*value=""(.+?)""");
        private static readonly Regex SizeRegex = new Regex(@"Размер: (.+)\.");
        private static readonly Regex QualityRegex = new Regex(@"Видео: (.+?)\.");

        private readonly char[] charsToReplace = { ' ', '-' };

        private HttpClient client;

        public List<TorrentDescription> GetEpisodeTorrents(Episode episode, string login, string password)
        {
            client = Login(login, password);
            string detailsContent = client.GetAsync(string.Format(DetailsUrl, episode.SiteId)).Result.Content.ReadAsStringAsync().Result;
            Match parametersMatch = ParametersRegex.Match(detailsContent);
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
        private static HttpClient Login(string login, string password)
        {
            CookieContainer cc = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cc,
                UseCookies = true,
                UseProxy = false
            };

            HttpClient httpClient = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, LoginUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "login", login },
                    { "password", password },
                    { "module", "1" },
                    { "target", "http://www.lostfilm.tv/" },
                    { "repage", "user" },
                    { "act", "login" }
                })
            };

            string response = httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            string url = UrlRegex.Match(response).Groups[1].Value;
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("An error occurred while retrieving URL. Login/password is probably incorrect.");
            }

            List<KeyValuePair<string, string>> formDictionary = response.Split('\n')
                .Where(s => InputRegex.IsMatch(s))
                .Select(s => new KeyValuePair<string, string>(InputRegex.Match(s).Groups[1].Value, InputRegex.Match(s).Groups[2].Value))
                .ToList();
            FormUrlEncodedContent content = new FormUrlEncodedContent(formDictionary);
            request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            httpClient.SendAsync(request).Wait();
            return httpClient;
        }

        private IEnumerable<TorrentDescription> GetTorrents(string showId, string seasonNumber, string episodeNumber)
        {
            string downloadsContent = Encoding.GetEncoding(1251).GetString(client.GetAsync(string.Format(DownloadsUrl, showId, seasonNumber, episodeNumber)).Result.Content.ReadAsByteArrayAsync().Result);
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
                TorrentDescription torrentDescription = new TorrentDescription()
                {
                    TorrentUri = new Uri(uri),
                    Description = description,
                    Size = SizeRegex.IsMatch(description) ? SizeRegex.Match(description).Groups[1].Value : string.Empty,
                    Quality = QualityRegex.IsMatch(description) ? QualityRegex.Match(description).Groups[1].Value : string.Empty
                };

                foreach (char c in charsToReplace)
                {
                    torrentDescription.Quality = torrentDescription.Quality.Replace(c, '_');
                }

                torrents.Add(torrentDescription);
            }

            return torrents;
        }
    }
}