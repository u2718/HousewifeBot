using System;
using System.Linq;
using DAL;
using User = DAL.User;
using System.Collections.Generic;
using Telegram;
using System.Text.RegularExpressions;

namespace HousewifeBot
{
    public class UnsubscribeCommand : Command
    {
        private const int MaxPageSize = 50;

        private static readonly Regex SuggestionNumRegex = new Regex(@"[0-9]+");

        public override void Execute()
        {
            int messageSize;
            int.TryParse(Arguments, out messageSize);
            if (messageSize == 0)
            {
                messageSize = MaxPageSize;
            }
            messageSize = Math.Min(messageSize, MaxPageSize);
            string response;

            User user;
            bool userHasSubscriptions = false;
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
                    Program.Logger.Debug($"{GetType().Name}: Checking if user has subscriptions");
                    try
                    {
                        userHasSubscriptions = db.Subscriptions.Any(s => s.User.Id == user.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while checking if user has subscriptions", e);
                    }
                }


                if (userHasSubscriptions)
                {
                    Show show=null;
                    List<Show> shows;
                    List<string> showsList;
                    if (string.IsNullOrEmpty(Arguments))
                    {
                        shows = user.Subscriptions.Select(s => s.Show).ToList();
                        showsList = shows.Select(s => "/" + s.Id + " " + s.Title + " (" + s.OriginalTitle + ")").ToList();

                        List<string> pagesList = new List<string>();
                        for (int i = 0; i < showsList.Count; i += messageSize)
                        {
                            if (i > showsList.Count)
                            {
                                break;
                            }

                            int count = Math.Min(showsList.Count - i, messageSize);
                            pagesList.Add(
                                showsList.GetRange(i, count)
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
                                    page = "Сериалы, на которые вы подписаны:\n" + page + "\n/next or /stop";
                                }

                                if (i == pagesList.Count - 1)
                                {
                                    page = "Сериалы, на которые вы подписаны:\n" + page + "\n/stop"; ;
                                }

                                TelegramApi.SendMessage(Message.From, page);

                                Message message;
                                string footerText = "";
                                do
                                {
                                    message = TelegramApi.WaitForMessage(Message.From);
                                    if (message?.Text != "/stop" && message?.Text != "/next")
                                    {
                                        string someMatch = SuggestionNumRegex.Match(message.Text).Groups[0].Value;
                                        if (someMatch == "" || someMatch == null)
                                        {
                                            if (i == pagesList.Count - 1)
                                            {
                                                footerText = "\n/stop";
                                            }
                                            else
                                            {
                                                footerText = "\n/next or /stop";
                                            }
                                            TelegramApi.SendMessage(Message.From, footerText);
                                        }
                                        else
                                        {
                                            int sId = int.Parse(someMatch);
                                            show = db.GetShowById(sId);
                                            break;
                                        }
                                    }
                                } while (message?.Text != "/stop" && message?.Text != "/next");

                                if (message.Text == "/stop" || show != null)
                                {
                                    break;
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{GetType().Name}: An error occurred while sending shows list", e);
                        }
                        
                    }

                    Program.Logger.Info($"{GetType().Name}: {Message.From} is trying to unsubscribe");


                        do
                        {
                            if (show == null)
                            {
                                Program.Logger.Info($"{GetType().Name}: Show was not selected");
                                response = $"Сериал не выбран";
                                break;
                            }

                            Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                            Subscription subscription;
                            try
                            {
                                subscription = db.Subscriptions
                                    .FirstOrDefault(s => s.User.Id == user.Id && s.Show.Id == show.Id);
                            }
                            catch (Exception e)
                            {
                                throw new Exception(
                                    $"{GetType().Name}: An error occurred while checking for subscription existence", e);
                            }

                            Program.Logger.Debug($"{GetType().Name}: Deleting notifications for subscription");
                            try
                            {
                                db.Notifications.RemoveRange(
                                    db.Notifications.Where(n => n.Subscription.Id == subscription.Id)
                                    );
                            }
                            catch (Exception e)
                            {
                                throw new Exception(
                                    $"{GetType().Name}: An error occurred while deleting notifications for subscription", e);
                            }

                            Program.Logger.Debug($"{GetType().Name}: Deleting subscription");
                            try
                            {
                                db.Subscriptions.Remove(subscription);
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"{GetType().Name}: An error occurred while deleting subscription", e);
                            }

                            response = $"Вы отписались от сериала '{show.Title}'";
                        } while (false);

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                        }

                }
                else
                {
                    response = "Вы не подписаны ни на один сериал";
                }

            }
            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}d: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
        }
    }
}
