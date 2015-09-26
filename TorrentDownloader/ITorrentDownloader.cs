using System;

namespace TorrentDownloader
{
    public interface ITorrentDownloader
    {
        bool Download(Uri torrent, Uri torrenWebUiUri, string password);
    }
}