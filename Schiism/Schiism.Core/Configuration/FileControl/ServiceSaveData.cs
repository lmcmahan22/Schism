using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Configuration.FileControl
{
    public class ServiceSaveData
    {
        // private variables
        private bool autoStart;
        private bool autoRestart;

        public ServiceSaveData(bool autoStart, bool autoRestart)
        {
            this.autoStart = autoStart;
            this.autoRestart = autoRestart;
        }

        public bool AutoStart { get => autoStart; set => autoStart = value; }

        public bool AutoRestart { get => autoRestart; set => autoRestart = value; }
    }
}
