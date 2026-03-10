using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class DataPoint
    {
        private string alias;
        private string data;

        public DataPoint(string a, string d)
        {
            this.alias = a;
            this.data = d;
        }

        public string Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        public string Data
        {
            get { return data; }
            set { data = value; }
        }
    }
}
