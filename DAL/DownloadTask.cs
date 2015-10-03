namespace DAL
{
    public class DownloadTask : DbEntry
    {
        public virtual Episode Episode { get; set; }
        public virtual User User { get; set; }
        public string TorrentUrl { get; set; }
        public bool DownloadStarted { get; set; } = false;
    }
}