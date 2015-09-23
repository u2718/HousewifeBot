using System;
using System.Linq;
using DAL;
using User = DAL.User;
using System.Collections.Generic;
using Telegram;
using System.Text.RegularExpressions;

namespace HousewifeBot
{
    public class SubscribeCommand : Command
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

            string showTitle;
            if (string.IsNullOrEmpty(Arguments))
            {
                Program.Logger.Debug($"{GetType().Name}: Sending 'Enter show title' prompt");
                try
                {
                    TelegramApi.SendMessage(Message.From, "Введите название сериала");
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while sending prompt", e);
                }

                Program.Logger.Debug($"{GetType().Name}: Waiting for a message that contains show title");
                try
                {
                    showTitle = TelegramApi.WaitForMessage(Message.From).Text;
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while waiting for a message that contains show title", e);
                }
            }
            else
            {
                showTitle = Arguments;
            }

            Program.Logger.Info($"{GetType().Name}: {Message.From} is trying to subscribe to '{showTitle}'");

            string response;
            using (AppDbContext db = new AppDbContext())
            {
                Show show;
                List<Show> shows;

                List<string> showsList;

                Program.Logger.Debug($"{GetType().Name}: Searching show {showTitle} in database");
                try
                {
                    show = db.GetShowByTitle(showTitle);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while searching show {showTitle} in database", e);
                }
                if (show == null)
                {
                    try
                    {
                        shows = db.GetShowsFuzzy(showTitle);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while retrieving fuzzy shows list", e);
                    }

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
                                page = "Возможно вы имели в виду:\n" + page + "\n/next or /stop";
                            }

                            if (i == pagesList.Count - 1)
                            {
                                page = "Возможно вы имели в виду:\n" + page + "\n/stop"; ;
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
                    //response = "Возможно вы имели в виду:\n" + showsList;
                }
                if (show != null)
                {
                    Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                    User user;
                    try
                    {
                        user = db.GetUserByTelegramId(Message.From.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while searching user in database", e);
                    }
                    bool newUser = false;
                    if (user == null)
                    {
                        user = new User
                        {
                            TelegramUserId = Message.From.Id,
                            FirstName = Message.From.FirstName,
                            LastName = Message.From.LastName,
                            Username = Message.From.Username
                        };
                        newUser = true;
                    }

                    if (newUser)
                    {
                        Program.Logger.Info($"{GetType().Name}: {user} is new User");
                    }
                    else
                    {
                        Program.Logger.Debug($"{GetType().Name}: User {user} is already exist");
                    }

                    bool subscriptionExists;
                    Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                    try
                    {
                        subscriptionExists = user.Subscriptions.Any(s => s.Show.Id == show.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while checking for subscription existence", e);
                    }
                    if (subscriptionExists)
                    {
                        Program.Logger.Info($"{GetType().Name}: User {Message.From} is already subscribed to {show.OriginalTitle}");
                        response = $"Вы уже подписаны на сериал '{show.Title}'";
                    }
                    else
                    {
                        Subscription subscription = new Subscription
                        {
                            User = user,
                            Show = show,
                            SubscriptionDate = DateTimeOffset.Now
                        };

                        if (newUser)
                        {
                            user.Subscriptions.Add(subscription);
                            db.Users.Add(user);
                        }
                        else
                        {
                            db.Subscriptions.Add(subscription);
                        }
                        response = $"Вы подписались на сериал '{show.Title}'";
                        /*Program.Logger.Debug($"{GetType().Name}: Sending response of successful subscription {Message.From}");
                        try
                        {
                            TelegramApi.SendMessage(Message.From, $"Вы подписались на сериал '{show.Title}'");
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
                        }*/
                    }

                    Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
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
                    Program.Logger.Info($"{GetType().Name}: Show {showTitle} was not found");
                    response = $"Сериал '{showTitle}' не найден";
                }
            }

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
        }
    }
}
