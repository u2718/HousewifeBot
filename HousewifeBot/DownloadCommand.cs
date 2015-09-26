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
                Notification notification = db.GetNotificationById(NotificationId);
                if (notification == null)
                {
                    Status = false;
                    return;
                }
                ITorrentGetter torrentGetter = new LostFilmTorrentGetter();
                Settings settings = db.GetSettingsByUser(notification.Subscription.User);
                if (settings == null)
                {
                    Status = false;
                    return;
                }
                List<TorrentDescription> torrents = torrentGetter.GetEpisodeTorrents(notification.Episode, settings.SiteLogin, settings.SitePassword);
                TorrentDescription torrent = torrents.FirstOrDefault(t => t.Quality == Quality);

                if (torrent == null)
                {
                    Status = false;
                    return;
                }
                db.DownloadTasks.Add(new DownloadTask()
                {
                    Episode = notification.Episode,
                    User = notification.Subscription.User,
                    TorrentUrl = torrent.TorrentUri.ToString()
                });
                db.SaveChanges();
                Status = true;
            }
        }
    }
}