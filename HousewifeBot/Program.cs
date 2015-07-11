using System.IO;
using Telegram;

namespace HousewifeBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var token = File.ReadAllText(@"token.txt");
            TelegramApi tg = new TelegramApi(token);
            User botUser = tg.GetMe();
        }
    }
}
