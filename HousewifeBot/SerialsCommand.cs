using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DAL;
using Telegram;

namespace HousewifeBot
{
    class SerialsCommand : Command
    {
        public SerialsCommand(TelegramApi telegramApi, Message message) : base(telegramApi, message)
        {
        }

        public SerialsCommand()
        {
        }

        public override bool Execute()
        {
            int pageSize = 10;
            List<string> serials;
            using (var db = new AppDbContext())
            {
                serials = db.Shows.Select(s => s.Title).ToList();
            }

            List<string> pagesList = new List<string>();
            for (int i = 0; i < serials.Count; i += pageSize)
            {
                if (i > serials.Count)
                {
                    break;
                }

                int count = Math.Min(serials.Count - i, pageSize);
                pagesList.Add(
                    serials.GetRange(i, count)
                    .Aggregate("", (s, s1) => s + "\n" + s1)
                    );
            }

            for (int i = 0; i < pagesList.Count; i++)
            {
                string page = pagesList[i];

                if (i != pagesList.Count - 1)
                {
                    page += "\n/next or /stop";
                }
                TelegramApi.SendMessage(Message.From, page);

                Message message;
                do
                {
                    message = TelegramApi.WaitForMessage(Message.From);
                    if (message?.Text != "/stop" && message?.Text != "/next")
                    {
                        TelegramApi.SendMessage(Message.From, "\n/next or /stop");
                    }
                } while (message?.Text != "/stop" && message?.Text != "/next");

                if (message.Text == "/stop")
                {
                    break;
                }
            }

            return true;
        }
    }
}
