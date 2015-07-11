using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace Telegram
{
    public class TelegramApi
    {
        private const string BotUrl = @"https://api.telegram.org/bot{0}/{1}";

        private readonly string _token;
        private readonly WebClient _webClient = new WebClient();
        private int _retryCount;

        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value < 1 ? 1 : value; }
        }

        public int Offset { get; private set; }

        public TelegramApi(string token, int offset = 0)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;
            RetryCount = 1;
            Offset = offset;
        }

        public User GetMe()
        {
            return ExecuteMethod<User>("GetMe");
        }

        public IEnumerable<Update> GetUpdates()
        {
            IEnumerable<Update> updates = ExecuteMethod<List<Update>>("getUpdates",
                new Dictionary<string, object>()
                {
                    {"offset", Offset}
                });


            Offset = updates.Any() ? updates.Last().UpdateId + 1 : Offset;
            return updates;
        }

        public Message SendMessage(int chatId, string text)
        {
            return ExecuteMethod<Message>("sendMessage",
                new Dictionary<string, object>()
                {
                    {"chat_id", chatId},
                    {"text", text}
                });
        }

        private T ExecuteMethod<T>(string method, Dictionary<string, object> parameters = null) where T : new()
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (parameters != null)
            {
                _webClient.QueryString.Add(
                    parameters.Aggregate(
                        new NameValueCollection(),
                        (nameValueCollection, p) =>
                        {
                            nameValueCollection.Add(p.Key, p.Value.ToString());
                            return nameValueCollection;
                        }
                        )
                    );
            }

            string response = string.Empty;
            for (int i = 0; i <= RetryCount; i++)
            {
                try
                {
                    response = _webClient.DownloadString(string.Format(BotUrl, _token, method));
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(1000);

                    if (i == RetryCount)
                    {
                        throw;
                    }
                }
                finally
                {
                    _webClient.QueryString.Clear();
                }
            }

            if (string.IsNullOrEmpty(response))
            {
                return new T();
            }

            Response<T> t = JsonConvert.DeserializeObject<Response<T>>(response);

            if (t.Result == null)
            {
                throw new Exception($"Method {method} returned 'Ok' = {t.Ok} and 'Result' = null");
            }

            if (!t.Ok)
            {
                throw new Exception($"Method {method} returned 'Ok' = false");
            }
            return t.Result;
        }
    }
}
