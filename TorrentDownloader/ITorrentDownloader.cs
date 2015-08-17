using System;
using System.Collections.Generic;
using DAL;

namespace TorrentDownloader
{
    public interface ITorrentDownloader
    {
        List<Uri> GetEpisodeTorrents(Episode episode, string login, string password);
    }
}