using System;
using System.IO;
using System.Threading;
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

            while (true)
            {
                foreach (var update in tg.GetUpdates())
                {
                    tg.SendMessage(update.Message.From.Id, "Test");
                }
                Thread.Sleep(200);
            }
        }
    }
}
