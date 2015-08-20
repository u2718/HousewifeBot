using System;
using System.Collections.Generic;
using DAL;

namespace TorrentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ITorrentDownloader downloader = new UTorrentDownloader();
            ITorrentGetter torrentGetter = new LostFilmTorrentGetter();

            List<Uri> torrents = torrentGetter.GetEpisodeTorrents(new Episode() { SiteId = 14949 }, "", "");
            torrents.ForEach(t => downloader.Download(t, new Uri("http://localhost:8081/gui/"), ""));
        }
    }
}
