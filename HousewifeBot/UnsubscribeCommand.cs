using System;
using System.Collections.Generic;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class UnsubscribeCommand : Command
    {
        public const string UnsubscribeCommandFormat = "/u_{0}";
        private const int MaxPageSize = 50;
        public int? ShowId { get; set; }

        public UnsubscribeCommand(int? showId = null)
        {
            ShowId = showId;
        }
       
        public override void Execute()
        {
            string response = null;
            do
            {
                User user = GetUser(Message.From);
                if (user == null || !UserHasSubscriptions(user))
                {
                    response = "Вы не подписаны ни на один сериал";
                    break;
                }

                if (ShowId != null)
                {
                    Show show;
                    using (AppDbContext db = new AppDbContext())
                    {
                        show = db.GetShowById(ShowId.Value);
                    }
                    if (show == null)
                    {
                        response = $"Сериал с идентификатором {ShowId} не найден";
                        break;
                    }
                    response = Unsubscribe(user, show);
                }
                else
                {
                    int messageSize;
                    int.TryParse(Arguments, out messageSize);
                    if (messageSize == 0)
                    {
                        messageSize = MaxPageSize;
                    }
                    messageSize = Math.Min(messageSize, MaxPageSize);
                    SendSubscriptions(user, messageSize);
                }
            } while (false);

            if (!string.IsNullOrEmpty(response))
            {
                Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
                try
                {
                    TelegramApi.SendMessage(Message.From, response);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
                }
            }
            Status = true;
        }
        private bool UserHasSubscriptions(User user)
        {
            if (user == null)
            {
                Program.Logger.Debug($"{GetType().Name}: User {Message.From} is not exists");
                return false;
            }

            bool userHasSubscriptions;
            using (AppDbContext db = new AppDbContext())
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
            return userHasSubscriptions;
        }
        private User GetUser(Telegram.User user)
        {
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {user.Id} in database");
                try
                {
                    return db.GetUserByTelegramId(user.Id);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while searching user in database", e);
                }
            }
        }
        private string Unsubscribe(User user, Show show)
        {
            string response;
            using (AppDbContext db = new AppDbContext())
            {
                Subscription subscription;
                Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                try
                {
                    subscription = db.Subscriptions.FirstOrDefault(s => s.User.Id == user.Id && s.Show.Id == show.Id);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while checking for subscription existence", e);
                }
                if (subscription != null)
                {
                    Program.Logger.Debug($"{GetType().Name}: Deleting notifications for subscription");
                    try
                    {
                        db.Notifications.RemoveRange(db.Notifications.Where(n => n.Subscription.Id == subscription.Id));
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while deleting notifications for subscription", e);
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

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                    }
                    response = $"Вы отписались от сериала \"{show.Title}\"";
                }
                else
                {
                    response = $"Вы, братишка, не подписаны на сериал \"{show.Title}\"";
                }
            }
            return response;
        }

        private void SendSubscriptions(User user, int messageSize)
        {
            List<string> showsList;
            using (AppDbContext db = new AppDbContext())
            {
                List<Show> shows = db.Subscriptions
                    .Where(s => s.User.Id == user.Id)
                    .Select(s => s.Show)
                    .ToList();
                showsList = shows.Select(s => $"{string.Format(UnsubscribeCommandFormat, s.Id)} {s.Title} ({s.OriginalTitle})").ToList();
            }

            List<string> pagesList = GetPages(showsList, messageSize);
            SendPages(pagesList);
        }
    }
}
