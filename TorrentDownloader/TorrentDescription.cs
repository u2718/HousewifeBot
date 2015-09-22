using System;

namespace TorrentDownloader
{
    public class TorrentDescription
    {
        public Uri TorrentUri { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public string Quality { get; set; }
    }
}