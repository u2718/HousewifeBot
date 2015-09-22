using System;
using System.Collections.Generic;
using DAL;

namespace TorrentDownloader
{
    public interface ITorrentGetter
    {
        List<TorrentDescription> GetEpisodeTorrents(Episode episode, string login, string password);
    }
}