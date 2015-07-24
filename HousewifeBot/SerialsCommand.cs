using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using Telegram;

namespace HousewifeBot
{
    class SerialsCommand : Command
    {
        private const int MaxPageSize = 50;

        public SerialsCommand(TelegramApi telegramApi, Message message) : base(telegramApi, message)
        {
        }

        public SerialsCommand()
        {
        }

        public override bool Execute()
        {
            Program.Logger.Debug($"SerialsCommand: Parsing message size. Arguments: {Arguments}");
            int messageSize;
            int.TryParse(Arguments, out messageSize);
            if (messageSize == 0)
            {
                messageSize = MaxPageSize;
            }
            messageSize = Math.Min(messageSize, MaxPageSize);
            Program.Logger.Debug($"SerialsCommand: Message size: {messageSize}");

            List<string> serials;

            Program.Logger.Debug($"SerialsCommand: Retrieving serials list");
            using (var db = new AppDbContext())
            {
                try
                {
                    serials = db.Shows.Select(s => s.Title + " (" + s.OriginalTitle + ")").ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("SerialsCommand: An error occurred while retrieving serials list", e);
                }
            }


            List<string> pagesList = new List<string>();
            for (int i = 0; i < serials.Count; i += messageSize)
            {
                if (i > serials.Count)
                {
                    break;
                }

                int count = Math.Min(serials.Count - i, messageSize);
                pagesList.Add(
                    serials.GetRange(i, count)
                    .Aggregate("", (s, s1) => s + "\n" + s1)
                    );
            }

            try
            {
                Program.Logger.Debug("SerialsCommand: Sending serials list");

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
                throw new Exception("SerialsCommand: An error occurred while sending serials list", e);
            }

            Status = true;
            return true;
        }
    }
}
