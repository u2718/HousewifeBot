using System;
using System.Linq;
using DAL;
using User = DAL.User;
using System.Collections.Generic;
using Telegram;

namespace HousewifeBot
{
    public class SubscribeCommand : Command
    {
        public const string SubscribeCommandFormat = "/s_{0}";
        private const int MaxPageSize = 50;

        public int? ShowId { get; set; }

        public SubscribeCommand()
        {

        }
        public SubscribeCommand(int showId)
        {
            ShowId = showId;
        }

        public override void Execute()
        {
            Show show;
            string showTitle = string.Empty;
            bool subscribeById = ShowId != null;
            if (ShowId == null)
            {
                int messageSize;
                int.TryParse(Arguments, out messageSize);
                if (messageSize == 0)
                {
                    messageSize = MaxPageSize;
                }
                messageSize = Math.Min(messageSize, MaxPageSize);

                ShowId = RequestShow(out showTitle);
                if (ShowId == null)
                {
                    SendShowList(showTitle, messageSize);
                    Status = true;
                    return;
                }
            }

            string response;
            Program.Logger.Info($"{GetType().Name}: {Message.From} is trying to subscribe to '{(subscribeById ? $"Id = {ShowId}" : showTitle)}'");
            using (AppDbContext db = new AppDbContext())
            {
                show = db.GetShowById(ShowId.Value);
                if (show == null)
                {
                    Program.Logger.Info($"{GetType().Name}: Show '{(subscribeById ? $"Id = {ShowId}" : showTitle)}' was not found");
                    response = $"Сериал '{(subscribeById ? $"Id = {ShowId}" : showTitle)}' не найден";
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
                    return;
                }

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

        private int? RequestShow(out string showTitle)
        {
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

            Show show;
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching show {showTitle} in database");
                try
                {
                    show = db.GetShowByTitle(showTitle);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while searching show {showTitle} in database", e);
                }
            }
            return show?.Id;
        }

        private void SendShowList(string showTitle, int messageSize)
        {
            using (AppDbContext db = new AppDbContext())
            {
                List<Show> shows;
                try
                {
                    shows = db.GetShowsFuzzy(showTitle);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving fuzzy shows list", e);
                }

                List<string> showsList = shows.Select(show => $"{string.Format(SubscribeCommandFormat, show.Id)} {show.Title} ({show.OriginalTitle})").ToList();

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
            }
        }
    }
}
