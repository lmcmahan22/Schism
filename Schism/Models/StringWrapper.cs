using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class StringWrapper
    {
        private string _value;

        public StringWrapper(string v)
        {
            this._value = v;
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
