using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class DataPointList : ObservableCollection<DataPoint>
    {
        public DataPointList() : base()
        {
            // Default constructor with no elements inside the list yet
        }
    }
}
