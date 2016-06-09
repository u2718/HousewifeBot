using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using NLog;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class Notifier
    {
        private static readonly Logger Logger = LogManager.GetLogger("Notifier");

        private TelegramApi telegramApi;

        public Notifier(TelegramApi telegramApi)
        {
            this.telegramApi = telegramApi;
        }

        public void UpdateNotifications()
        {
            int newNotificationsCount = CreateEpisodeNotifications();
            if (newNotificationsCount != 0)
            {
                Logger.Info($"UpdateNotifications: {newNotificationsCount} new " +
                            $"{((newNotificationsCount == 1) ? "notification was created" : "notifications were created")}");
            }

            newNotificationsCount = CreateShowNotifications();
            if (newNotificationsCount != 0)
            {
                Logger.Info($"UpdateNotifications: {newNotificationsCount} new " +
                            $"{((newNotificationsCount == 1) ? "show notification was created" : "show notifications were created")}");
            }
        }

        public void SendShowsNotifications()
        {
            using (AppDbContext db = new AppDbContext())
            {
                var showNotifications = db.ShowNotifications
                    .Where(n => !n.Notified)
                    .ToList();
                if (showNotifications.Count == 0)
                {
                    return;
                }

                Logger.Debug("SendShowsNotifications: Sending new notifications");
                foreach (var notification in showNotifications)
                {
                    string text = $"{notification.Show.Title} ({string.Format(SubscribeCommand.SubscribeCommandFormat, notification.Show.Id)})\n{notification.Show.Description}";
                    try
                    {
                        telegramApi.SendMessage(notification.User.TelegramUserId, text);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "SendShowsNotifications: An error occurred while sending new notifications");
                    }

                    notification.Notified = true;
                }

                Logger.Trace("SendShowsNotifications: Saving changes to database");
                db.SaveChanges();
            }
        }

        public void SendEpisodesNotifications()
        {
            using (AppDbContext db = new AppDbContext())
            {
                var notifications = db.Notifications
                    .Where(n => !n.Notified)
                    .ToList();
                if (notifications.Count == 0)
                {
                    return;
                }

                var notificationDictionary =
                    notifications.Aggregate(
                        new Dictionary<User, List<Notification>>(),
                        (d, notification) =>
                        {
                            if (d.ContainsKey(notification.Subscription.User))
                            {
                                d[notification.Subscription.User].Add(notification);
                            }
                            else
                            {
                                d.Add(notification.Subscription.User, new List<Notification>() { notification });
                            }

                            return d;
                        });

                Logger.Debug("SendEpisodesNotifications: Sending new notifications");
                foreach (var userNotifications in notificationDictionary)
                {
                    string text = string.Empty;
                    foreach (Notification notification in userNotifications.Value)
                    {
                        text += notification.Subscription.Show.Title + " - " + notification.Episode.Title;
                        var settings = db.GetSettingsByUser(userNotifications.Key);
                        if (settings != null)
                        {
                            text = GetTorrents(notification, settings);
                        }
                    }

                    try
                    {
                        telegramApi.SendMessage(userNotifications.Key.TelegramUserId, text);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "SendEpisodesNotifications: An error occurred while sending new notifications");
                    }
                }

                Logger.Info($"SendEpisodesNotifications: {notificationDictionary.Count} new " +
                            $"{((notificationDictionary.Count == 1) ? "notification was sent" : "notifications were sent")}");
                notifications.ForEach(notification => { db.Notifications.First(n => n.Id == notification.Id).Notified = true; });
                Logger.Trace("SendEpisodesNotifications: Saving changes to database");
                db.SaveChanges();
            }
        }

        private static int CreateShowNotifications()
        {
            int newNotificationsCount = 0;
            using (AppDbContext db = new AppDbContext())
            {
                Logger.Trace("UpdateNotifications: Retrieving new shows for users");
                foreach (var user in db.Users)
                {
                    var shows = db.Shows
                        .Where(s => s.DateCreated > user.DateCreated &&
                                    !db.ShowNotifications.Any(n => n.User.Id == user.Id && n.Show.Id == s.Id))
                        .ToList();
                    if (shows.Count == 0)
                    {
                        continue;
                    }

                    var newNotifications = shows.Aggregate(
                        new List<ShowNotification>(), 
                        (list, show) =>
                        {
                            list.Add(new ShowNotification() { Show = show, User = user });
                            return list;
                        });
                    db.ShowNotifications.AddRange(newNotifications);
                    newNotificationsCount += newNotifications.Count;
                }

                Logger.Trace("UpdateNotifications: Saving changes to database");
                db.SaveChanges();
            }

            return newNotificationsCount;
        }

        private static int CreateEpisodeNotifications()
        {
            int newNotificationsCount = 0;
            using (AppDbContext db = new AppDbContext())
            {
                Logger.Trace("UpdateNotifications: Retrieving new episodes for subscriptions");
                foreach (Subscription subscription in db.Subscriptions)
                {
                    if (subscription.User == null || subscription.Show == null)
                    {
                        continue;
                    }

                    var notifications = db.Notifications.Where(n => n.Subscription.Id == subscription.Id);
                    var episodes = db.Episodes
                        .Where(s => s.Show.Id == subscription.Show.Id && s.Date >= subscription.SubscriptionDate &&
                                    !notifications.Any(n => n.Episode.Id == s.Id))
                        .Select(s => s).ToList();
                    if (episodes.Count == 0)
                    {
                        continue;
                    }

                    Logger.Debug("UpdateNotifications: Creating notifications for subcription: " +
                                 $" {subscription.User} -" +
                                 $" {subscription.Show.OriginalTitle}");

                    List<Notification> newNotifications = episodes.Aggregate(
                        new List<Notification>(),
                        (list, episode) =>
                        {
                            list.Add(new Notification
                            {
                                Episode = episode,
                                Subscription = subscription
                            });
                            return list;
                        });

                    db.Notifications.AddRange(newNotifications);
                    newNotificationsCount += newNotifications.Count;
                }

                Logger.Trace("UpdateNotifications: Saving changes to database");
                db.SaveChanges();
            }

            return newNotificationsCount;
        }

        private static string GetTorrents(Notification notification, Settings settings)
        {
            string text = string.Empty;
            var torrentGetter = new LostFilmTorrentGetter();
            try
            {
                List<TorrentDescription> torrents = torrentGetter.GetEpisodeTorrents(notification.Episode, settings.SiteLogin, settings.SitePassword);
                if (torrents.Count != 0)
                {
                    text += " (" +
                            torrents.Select(t => t.Quality)
                                .Aggregate(string.Empty, (s, s1) => s + " " + string.Format(DownloadCommand.DownloadCommandFormat, notification.Id, s1))
                            + ")";
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "SendEpisodesNotifications: An error occured while retrieving torrents for {notification.Episode.Show.Title} - {notification.Episode.Title}");
                text += " (Не удалось получить список торрентов. Возможно указан неверный логин/пароль)";
            }

            text += "\n";
            return text;
        }
    }
}
