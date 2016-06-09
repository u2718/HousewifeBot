using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using Telegram;

namespace HousewifeBot
{
    public class ShowsCommand : Command
    {
        private const int MaxPageSize = 50;

        public ShowsCommand(TelegramApi telegramApi, Message message) : base(telegramApi, message)
        {
        }

        public ShowsCommand()
        {
        }

        public override void Execute()
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

            List<string> pages = GetPages(shows, messageSize);
            SendPages(pages);
            Status = true;
        }
    }
}
