using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class MySubscriptionsCommand : Command
    {
        private const int MaxPageSize = 50;

        public override void Execute()
        {
            User user;
            List<Show> userShows = null;
            using (var db = new AppDbContext())
            {
                Program.Logger.Debug(
                    $"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                try
                {
                    user = db.GetUserByTelegramId(Message.From.Id);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while searching user in database", e);
                }

                if (user == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: User {Message.From} is not exists");
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: Retrieving user's subscriptions");
                    try
                    {
                        userShows = db.Subscriptions
                            .Where(s => s.User.Id == user.Id)
                            .Select(s => s.Show)
                            .ToList();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while retrieving user's subscriptions", e);
                    }
                }
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
                    throw new Exception(
                        $"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
                }

                Status = true;
                return;
            }

            List<string> pagesList = new List<string>();
            for (int i = 0; i < userShows.Count; i += MaxPageSize)
            {
                if (i > userShows.Count)
                {
                    break;
                }

                int count = Math.Min(userShows.Count - i, MaxPageSize);
                pagesList.Add(
                    userShows.GetRange(i, count)
                    .Aggregate("", (s, s1) => s + "\n" + $"- {s1.Title} ({s1.OriginalTitle})")
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

                    if (i == 0)
                    {
                        page = $"Вы, братишка, подписаны на следующие сериалы: {page}";
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
        }
    }
}