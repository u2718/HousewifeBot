using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    public class Settings : DbEntry
    {
        public virtual User User { get; set; }
        public string WebUiUrl { get; set; }
        public string _WebUiPassword { get; private set; }
        public string WebUiPasswordIV { get; set; }
        [NotMapped]
        public string WebUiPassword
        {
            get
            {
                return Encryptor.Decrypt(_WebUiPassword, WebUiPasswordIV);
            }
            set
            {
                string iv;
                _WebUiPassword = Encryptor.Encrypt(value, out iv);
                WebUiPasswordIV = iv;
            }
        }
        public string SiteLogin { get; set; }
        public string _SitePassword { get; private set; }
        public string SitePasswordIV { get; set; }
        [NotMapped]
        public string SitePassword
        {
            get
            {
                return Encryptor.Decrypt(_SitePassword, SitePasswordIV);
            }
            set
            {
                string iv;
                _SitePassword = Encryptor.Encrypt(value, out iv);
                SitePasswordIV = iv;
            }
        }
        public bool AutoDownload { get; set; }
    }
}
