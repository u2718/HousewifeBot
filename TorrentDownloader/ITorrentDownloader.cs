using System;

namespace TorrentDownloader
{
    public interface ITorrentDownloader
    {
        void Download(Uri torrent, Uri torrenWebUiUri, string password);
    }
}