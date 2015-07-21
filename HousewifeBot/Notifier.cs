using System.Collections.Generic;
using System.Linq;
using DAL;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class Notifier
    {
        public TelegramApi TelegramApi { get; set; }

        public Notifier(TelegramApi telegramApi)
        {
            TelegramApi = telegramApi;
        }

        public void UpdateNotifications()
        {
            using (AppDbContext db = new AppDbContext())
            {
                foreach (Subscription subscription in db.Subscriptions)
                {
                    List<Series> seriesList = db.Series
                        .Where(s => s.Show.Id == subscription.Show.Id && s.Date >= subscription.SubscriptionDate)
                        .Select(s => s).ToList();

                    foreach (Series series in seriesList)
                    {
                        Notification notification = new Notification
                        {
                            Series = series,
                            Subscription = subscription
                        };

                        bool notificationExists = db.Notifications.Any(n =>
                            n.Series.Id == series.Id && n.Subscription.Id == subscription.Id);
                        if (!notificationExists)
                        {
                            db.Notifications.Add(notification);
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        public void SendNotifications()
        {
            using (AppDbContext db = new AppDbContext())
            {
                List<Notification> notifications = db.Notifications.Where(n => !n.Notified)
                    .Select(n => n)
                    .ToList();
                if (notifications.Count == 0)
                {
                    return;
                }

                Dictionary<User, List<Notification>> notificationDictionary =
                    notifications.Aggregate(
                    new Dictionary<User, List<Notification>>(),
                    (d, n) =>
                    {
                        if (d.ContainsKey(n.Subscription.User))
                        {
                            d[n.Subscription.User].Add(n);
                        }
                        else
                        {
                            d.Add(n.Subscription.User, new List<Notification>() { n });
                        }
                        return d;
                    }
                    );

                foreach (var userNotifications in notificationDictionary)
                {
                    string text = string.Join(", ", userNotifications.Value
                        .Select(n => n.Subscription.Show.Title + " - " + n.Series.Title));
                    TelegramApi.SendMessage(userNotifications.Key.TelegramUserId, text);
                }

                notifications.ForEach(
                    notification =>
                    {
                        db.Notifications
                            .First(n => n.Id == notification.Id).Notified = true;
                    }
                    );
                db.SaveChanges();
            }
        }
    }
}
