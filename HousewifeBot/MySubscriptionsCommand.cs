using System;
using System.Collections.Generic;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class MySubscriptionsCommand : Command
    {
        private const int MaxPageSize = 50;

        public override void Execute()
        {
            List<Show> userShows = null;
            using (var db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");

                var user = db.GetUserByTelegramId(Message.From.Id);
                if (user == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: User {Message.From} is not exists");
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: Retrieving user's subscriptions");
                    userShows = user.Subscriptions.Select(s => s.Show).ToList();
                }

                if (userShows == null || userShows.Count == 0)
                {
                    Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
                    try
                    {
                        TelegramApi.SendMessage(Message.From, "Вы не подписаны ни на один сериал");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
                    }

                    Status = true;
                }
                else
                {
                    List<string> pages = GetPages(userShows.Select(s => $"- {s.Title} ({s.OriginalTitle}) {s.SiteType.Title}").ToList(), MaxPageSize);
                    SendPages(pages);
                    Status = true;
                }
            }
        }
    }
}