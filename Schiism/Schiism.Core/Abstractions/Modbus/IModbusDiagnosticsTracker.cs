using Schiism.Core.Models.DTOs.IPC_Records.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.Modbus
{
    public interface IModbusDiagnosticsTracker
    {
        void OnRequest();

        void OnSuccess();

        void OnError(Exception ex);

        ConnectionDiagnostics Snapshot();
    }
}
