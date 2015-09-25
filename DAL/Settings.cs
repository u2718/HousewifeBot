using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Settings : DbEntry
    {
        public virtual User User { get; set; }
        public string WebUiUrl { get; set; }
        public string WebUiPassword { get; set; }
        public string SiteLogin { get; set; }
        public string SitePassword { get; set; }
        public bool AutoDownload { get; set; }
    }
}
