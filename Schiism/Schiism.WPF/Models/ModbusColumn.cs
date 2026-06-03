using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.WPF.Models
{
    public class ModbusColumn : BindableBase
    {
        private double width = 100;

        public string Header { get; set; } = string.Empty;

        public double Width
        {
            get => width;
            set => SetProperty(ref width, value);
        }
    }
}
