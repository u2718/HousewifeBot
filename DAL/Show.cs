using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Show
    {
        public String Title { get; set; }
        public List<Series> SeriesList { get; set; }

        public Show()
        {
             SeriesList = new List<Series>();
        }
    }
}
