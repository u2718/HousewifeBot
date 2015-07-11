using System;
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

        public int RetryCount {
            get { return _retryCount; }
            set { _retryCount = value < 1 ? 1 : value; }
        }
        
        public TelegramApi(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;
            RetryCount = 1;
        }

        public User GetMe()
        {
            return ExecuteMethod<User>("GetMe");
        }

        private T ExecuteMethod<T>(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            string response = string.Empty;
            for (int i = 0; i < RetryCount; i++)
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
                }
            }

            if (string.IsNullOrEmpty(response))
            {
                return default(T);
            }

            Response<T> t = JsonConvert.DeserializeObject<Response<T>>(response);

            if (!t.Ok)
            {
                throw new Exception($"Method {method} returned OK = false");
            }
            return t.Result;
        }
    }
}
