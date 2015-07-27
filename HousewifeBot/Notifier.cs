﻿using System;
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

        public TelegramApi TelegramApi { get; set; }

        public Notifier(TelegramApi telegramApi)
        {
            TelegramApi = telegramApi;
        }

        public void UpdateNotifications()
        {
            int newNotificationsCount = 0;

            using (AppDbContext db = new AppDbContext())
            {
                Logger.Trace("UpdateNotifications: Retrieving subscriptions");
                List<Subscription> subscriptions;
                try
                {
                    subscriptions = db.Subscriptions.ToList();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "UpdateNotifications: An error occurred while retrieving subscriptions");
                    return;
                }

                Logger.Trace("UpdateNotifications: Retrieving new series for subscriptions");
                foreach (Subscription subscription in subscriptions)
                {
                    if (subscription.User == null || subscription.Show == null)
                    {
                        continue;
                    }

                    IQueryable<Notification> notifications;
                    try
                    {
                        notifications = db.Notifications
                            .Where(n => n.Subscription.Id == subscription.Id);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "UpdateNotifications: An error occurred while retrieving notifications for subscription: " +
                                        $" {subscription.User.FirstName} {subscription.User.LastName} -" +
                                        $" {subscription.Show.OriginalTitle}");
                        continue;
                    }

                    List<Series> seriesList;
                    try
                    {
                        seriesList = db.Series
                            .Where(s => s.Show.Id == subscription.Show.Id && s.Date >= subscription.SubscriptionDate &&
                            !notifications.Any(n => n.Series.Id == s.Id))
                            .Select(s => s).ToList();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "UpdateNotifications: An error occurred while retrieving subscriptions");
                        continue;
                    }

                    if (seriesList.Count == 0)
                    {
                        continue;
                    }

                    Logger.Debug("UpdateNotifications: Creating notifications for subcription: " +
                                 $" {subscription.User.FirstName} {subscription.User.LastName} -" +
                                 $" {subscription.Show.OriginalTitle}");

                    List<Notification> newNotifications = seriesList.Aggregate(
                        new List<Notification>(),
                        (list, series) =>
                        {
                            list.Add(new Notification
                            {
                                Series = series,
                                Subscription = subscription
                            });
                            return list;
                        }
                        );

                    try
                    {
                        db.Notifications.AddRange(newNotifications);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "UpdateNotifications: An error occurred while creating notifications");
                    }

                    newNotificationsCount += newNotifications.Count;
                }

                Logger.Trace("UpdateNotifications: Saving changes to database");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "UpdateNotifications: An error occurred while saving changes to database");
                }
            }

            if (newNotificationsCount != 0)
            {
                Logger.Info($"UpdateNotifications: {newNotificationsCount} new " +
                            $"{((newNotificationsCount == 1) ? "notification was created" : "notifications were created")}");
            }
        }

        public void SendNotifications()
        {
            using (AppDbContext db = new AppDbContext())
            {

                Logger.Trace("SendNotifications: Retrieving new notifications");
                List<Notification> notifications;
                try
                {
                    notifications = db.Notifications.Where(n => !n.Notified)
                        .Select(n => n)
                        .ToList();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "SendNotifications: An error occurred while retrieving new notifications");
                    return;
                }

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

                Logger.Debug("SendNotifications: Sending new notifications");
                foreach (var userNotifications in notificationDictionary)
                {
                    string text = string.Join(", ", userNotifications.Value
                        .Select(n => n.Subscription.Show.Title + " - " + n.Series.Title));

                    try
                    {
                        TelegramApi.SendMessage(userNotifications.Key.TelegramUserId, text);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "SendNotifications: An error occurred while sending new notifications");
                    }
                }

                Logger.Info($"SendNotifications: {notificationDictionary.Count} new " +
                            $"{((notificationDictionary.Count == 1) ? "notification was sent" : "notifications were sent")}");

                Logger.Debug("SendNotifications: Marking new notifications as notified");
                try
                {
                    notifications.ForEach(
                        notification =>
                        {
                            db.Notifications
                                .First(n => n.Id == notification.Id).Notified = true;
                        }
                        );
                }
                catch (Exception e)
                {
                    Logger.Error(e, "SendNotifications: An error occurred while marking new notifications as notified");
                }

                Logger.Trace("SendNotifications: Saving changes to database");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "SendNotifications: An error occurred while saving changes to database");
                }
            }
        }
    }
}
