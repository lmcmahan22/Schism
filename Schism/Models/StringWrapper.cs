using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
            set { _value = value;}
        }
    }
}