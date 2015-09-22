using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DAL;

namespace TorrentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ITorrentDownloader downloader = new UTorrentDownloader();
            ITorrentGetter torrentGetter = new LostFilmTorrentGetter();

            StreamWriter sw = new StreamWriter(@"E:\torrents.txt");

            using (AppDbContext db = new AppDbContext())
            {
                List<TorrentDescription> torrents = new List<TorrentDescription>();
                foreach (var episode in db.Episodes)
                {
                    List<TorrentDescription> t; 
                    try
                    {
                        t = torrentGetter.GetEpisodeTorrents(episode, "", "");
                        torrents.AddRange(t);
                        t.ForEach(a => sw.Write($"EpisodeId: {episode.SiteId}; Quality: {a.Quality}; Size: {a.Size}; Description: {a.Description}\n"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                     
                    
                    
                }
            }
            //torrents.ForEach(t => downloader.Download(t, new Uri("http://localhost:8081/gui/"), ""));
        }
    }
}
