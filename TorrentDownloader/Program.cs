using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TorrentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ITorrentDownloader torrentDownloader = new LostFilmTorrentDownloader();
            torrentDownloader.GetEpisodeTorrents(null, "", "");
        }
    }
}
