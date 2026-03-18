using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class MODBUSService
    {

        // Singleton instance
        private static readonly Lazy<MODBUSService> _instance = new(() => new MODBUSService());
        public static MODBUSService Instance => _instance.Value;

        // Constructor
        public MODBUSService()
        { 
        
        }
    }
}
