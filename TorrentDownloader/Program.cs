using System;
using System.Collections.Generic;
using DAL;

namespace TorrentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ITorrentDownloader torrentDownloader = new LostFilmTorrentDownloader();
            List<Uri> torrents = torrentDownloader.GetEpisodeTorrents(new Episode() { SiteId = 14949 }, "", "");
        }
    }
}
