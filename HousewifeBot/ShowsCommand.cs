using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using Telegram;

namespace HousewifeBot
{
    class ShowsCommand : Command
    {
        private const int MaxPageSize = 50;

        public ShowsCommand(TelegramApi telegramApi, Message message) : base(telegramApi, message)
        {
        }

        public ShowsCommand()
        {
        }

        public override bool Execute()
        {
            Program.Logger.Debug($"{GetType().Name}: Parsing message size. Arguments: {Arguments}");
            int messageSize;
            int.TryParse(Arguments, out messageSize);
            if (messageSize == 0)
            {
                messageSize = MaxPageSize;
            }
            messageSize = Math.Min(messageSize, MaxPageSize);
            Program.Logger.Debug($"{GetType().Name}: Message size: {messageSize}");

            List<string> shows;

            Program.Logger.Debug($"{GetType().Name}: Retrieving shows list");
            using (var db = new AppDbContext())
            {
                try
                {
                    shows = db.Shows.Select(s => s.Title + " (" + s.OriginalTitle + ")").ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving shows list", e);
                }
            }


            List<string> pagesList = new List<string>();
            for (int i = 0; i < shows.Count; i += messageSize)
            {
                if (i > shows.Count)
                {
                    break;
                }

                int count = Math.Min(shows.Count - i, messageSize);
                pagesList.Add(
                    shows.GetRange(i, count)
                    .Aggregate("", (s, s1) => s + "\n" + s1)
                    );
            }

            try
            {
                Program.Logger.Debug($"{GetType().Name}: Sending shows list");

                for (int i = 0; i < pagesList.Count; i++)
                {
                    string page = pagesList[i];

                    if (i != pagesList.Count - 1)
                    {
                        page += "\n/next or /stop";
                    }
                    TelegramApi.SendMessage(Message.From, page);

                    if (i == pagesList.Count - 1)
                    {
                        break;
                    }
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
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending shows list", e);
            }

            Status = true;
            return true;
        }
    }
}
