using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DAL;

namespace HousewifeBot
{
    public class SubscribeCommand : Command
    {
        public const string SubscribeCommandFormat = "/s_{0}";
        private const int MaxPageSize = 50;

        public SubscribeCommand()
        {

        }

        public SubscribeCommand(int showId)
        {
            ShowId = showId;
        }

        public int? ShowId { get; set; }

        public override void Execute()
        {
            Show show;
            string showTitle = string.Empty;
            bool subscribeById = ShowId != null;
            if (ShowId == null)
            {
                bool showFound = RequestShow(out showTitle);
                if (!showFound)
                {
                    SendShowList(showTitle, GetMessageSize());
                }

                Status = true;
                return;
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
                var user = db.GetUserByTelegramId(Message.From.Id);
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

                Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                var subscriptionExists = user.Subscriptions.Any(s => s.Show.Id == show.Id);
                if (subscriptionExists)
                {
                    Program.Logger.Info($"{GetType().Name}: User {Message.From} is already subscribed to {show.OriginalTitle}");
                    response = $"Вы уже подписаны на сериал \"{show.Title}\" ({show.SiteType.Title})";
                }
                else
                {
                    Subscribe(db, user, show, newUser);
                    response = $"Вы подписались на сериал \"{show.Title}\" ({show.SiteType.Title})";
                }

                Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                db.SaveChanges();
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

        private static void Subscribe(AppDbContext db, User user, Show show, bool newUser)
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
        }

        private int GetMessageSize()
        {
            int messageSize;
            int.TryParse(Arguments, out messageSize);
            if (messageSize == 0)
            {
                messageSize = MaxPageSize;
            }

            messageSize = Math.Min(messageSize, MaxPageSize);
            return messageSize;
        }

        private bool RequestShow(out string showTitle)
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

            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching show {showTitle} in database");
                var shows = db.GetShowsByTitle(showTitle);
                if (shows == null || shows.Count == 0)
                {
                    return false;
                }

                var subscriptionCommands = string.Join("; ", shows.Select(s => $"{s.SiteType.Title}: {string.Format(SubscribeCommandFormat, s.Id)}"));
                TelegramApi.SendMessage(Message.From, subscriptionCommands);
                return true;
            }
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

                List<string> showsList = shows.Select(show => $"{string.Format(SubscribeCommandFormat, show.Id)} {show.Title} ({show.OriginalTitle}) {show.SiteType.Title}").ToList();
                List<string> pages = GetPages(showsList, messageSize);
                SendPages(pages);
            }
        }
    }
}
