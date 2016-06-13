using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using DAL;
using NLog;

namespace TorrentDownloader
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            Logger.Info($"TorrentDownloader started: {Assembly.GetEntryAssembly().Location}");
            ITorrentDownloader downloader = new QBittorrentDownloader();
            int updateInterval;
            try
            {
                updateInterval = int.Parse(ConfigurationManager.AppSettings["UpdateInterval"]) * 1000;
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading update interval");
                return;
            }

            do
            {
                using (AppDbContext db = new AppDbContext())
                {
                    var downloadTasks = db.DownloadTasks
                        .Where(d => !d.DownloadStarted)
                        .ToList();

                    if (downloadTasks.Count != 0)
                    {
                        Logger.Debug($"Awaiting download tasks number: {downloadTasks.Count}");
                    }

                    foreach (var downloadTask in downloadTasks)
                    {
                        Settings setting;
                        try
                        {
                            setting = db.GetSettingsByUser(downloadTask.User);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"An error occurred while loading settings of {downloadTask.User}");
                            continue;
                        }
                        if (setting == null)
                        {
                            Logger.Debug($"Settings of {downloadTask.User} was not found");
                            continue;
                        }

                        bool downloadStarted;
                        Logger.Debug($"Downloading torrent {downloadTask.TorrentUrl}");
                        try
                        {
                            downloadStarted = downloader.Download(new Uri(downloadTask.TorrentUrl), new Uri(setting.WebUiUrl), setting.WebUiPassword);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "An error occurred while downloading torrent");
                            downloadStarted = false;
                        }

                        downloadTask.DownloadStarted = downloadStarted;
                        db.SaveChanges();
                    }
                }
                Thread.Sleep(updateInterval);
            } while (true);
        }
    }
}
