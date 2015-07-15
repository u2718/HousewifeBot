using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Subscription : DbEntry
    {
        public virtual User User { get; set; }
        public virtual Show Show { get; set; }
        public DateTime SubscriptionDate { get; set; }
    }
}
