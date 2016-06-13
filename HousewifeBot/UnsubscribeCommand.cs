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

        public UnsubscribeCommand(int? showId = null)
        {
            ShowId = showId;
        }

        public int? ShowId { get; set; }

        public override void Execute()
        {
            using (var db = new AppDbContext())
            {
                User user = db.GetUserByTelegramId(Message.From.Id);
                if (user == null || !user.Subscriptions.Any())
                {
                    SendResponse("Вы не подписаны ни на один сериал");
                    Status = true;
                    return;
                }

                if (ShowId != null)
                {
                    var show = db.GetShowById(ShowId.Value);
                    if (show == null)
                    {
                        SendResponse($"Сериал с идентификатором {ShowId} не найден");
                    }
                    else
                    {
                        var response = Unsubscribe(user, show);
                        SendResponse(response);
                    }
                }
                else
                {
                    SendSubscriptions(user, GetMessageSize());
                }
            }

            Status = true;
        }

        private void SendResponse(string response)
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

                    response = $"Вы отписались от сериала \"{show.Title}\" ({show.SiteType.Title})";
                }
                else
                {
                    response = $"Вы, братишка, не подписаны на сериал \"{show.Title}\" ({show.SiteType.Title})";
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
