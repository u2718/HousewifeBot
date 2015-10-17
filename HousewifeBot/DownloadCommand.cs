using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DAL;

namespace HousewifeBot
{
    public class DownloadCommand : Command
    {
        public const string DownloadCommandFormat = "/d{0}_{1}";

        public int NotificationId { get; }
        public string Quality { get; }
        public DownloadCommand(int notificationId, string quality)
        {
            NotificationId = notificationId;
            Quality = quality;
        }

        public override void Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Retrieving notification with id: {NotificationId}");
                Notification notification = null;
                try
                {
                    notification = db.GetNotificationById(NotificationId);
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, $"{GetType().Name}: An error occurred while retrieving notification");
                }

                if (notification == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: Notification with specified Id was not found");
                    Status = false;
                    return;
                }

                Program.Logger.Debug($"{GetType().Name}: Retrieving settings of {notification.Subscription.User}");
                Settings settings = null;
                try
                {
                    settings = db.GetSettingsByUser(notification.Subscription.User);
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, "An error occurred while retrieving user's settings");
                }

                if (settings == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: User's settings were not found");
                    Status = false;
                    return;
                }

                ITorrentGetter torrentGetter = new LostFilmTorrentGetter();

                List<TorrentDescription> torrents = null;
                Program.Logger.Debug($"{GetType().Name}: Retrieving torrents for {notification.Episode.Show} - {notification.Episode.Title}");
                try
                {
                    torrents = torrentGetter.GetEpisodeTorrents(notification.Episode, settings.SiteLogin, settings.SitePassword);
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, $"An error occured while retrieving torrents for {notification.Episode.Show.Title} - {notification.Episode.Title}");
                    TelegramApi.SendMessage(Message.From, "(Не удалось получить список торрентов. Возможно указан неверный логин/пароль)");
                    Status = false;
                    return;
                }
                Program.Logger.Debug($"{GetType().Name}: Number of torrents: {torrents?.Count() ?? 0}");

                TorrentDescription torrent = null;
                if (torrents != null && torrents.Count() != 0)
                {
                    Program.Logger.Debug($"{GetType().Name}: Retrieving torrent with required quality ({Quality})");
                    torrent = torrents.FirstOrDefault(t => t.Quality == Quality);
                }

                if (torrent == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: Torrent with required quality was not found");
                    Status = false;
                    return;
                }

                Program.Logger.Debug($"{GetType().Name}: Creating new download task");
                db.DownloadTasks.Add(new DownloadTask()
                {
                    Episode = notification.Episode,
                    User = notification.Subscription.User,
                    TorrentUrl = torrent.TorrentUri.ToString()
                });

                Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                }
                Status = true;
            }
        }
    }
}