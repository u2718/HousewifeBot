using System.Collections.Generic;
using DAL;

namespace HousewifeBot
{
    public interface ITorrentGetter
    {
        List<TorrentDescription> GetEpisodeTorrents(Episode episode, string login, string password);
    }
}