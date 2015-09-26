using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DAL;

namespace TorrentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ITorrentDownloader downloader = new UTorrentDownloader();

            do
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var settings = db.Settings.ToList();
                    var downloadTasks = db.DownloadTasks
                        .Where(d => !d.DownloadStarted)
                        .ToList();

                    foreach (var downloadTask in downloadTasks)
                    {
                        var setting = settings.FirstOrDefault(s => s.User.Id == downloadTask.User.Id);
                        if (setting == null)
                        {
                            continue;
                        }

                        downloadTask.DownloadStarted = downloader.Download(new Uri(downloadTask.TorrentUrl), new Uri(setting.WebUiUrl), setting.WebUiPassword);
                        db.SaveChanges();
                    }
                }
                Thread.Sleep(10000);
            } while (true);
        }
    }
}
