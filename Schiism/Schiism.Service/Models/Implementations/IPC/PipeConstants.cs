using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Service.Models.Implementations.IPC
{
    public static class PipeConstants
    {
        public const string ModbusDataStreamName = "schiism.modbusData.stream.v1";
        public const string ConnDiagStreamName = "schiism.connDiag.stream.v1";
        public const string SettingsCommandName = "schiism.settings.cmd.v1";
    }
}
