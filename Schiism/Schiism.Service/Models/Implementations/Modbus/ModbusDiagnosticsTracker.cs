using NModbus;
using Schiism.Core.Abstractions.Modbus;
using Schiism.Core.Models.DTOs.IPC_Records.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Service.Models.Implementations.Modbus
{
    public class ModbusDiagnosticsTracker : IModbusDiagnosticsTracker
    {
        private readonly ConnectionDiagnostics comms = new();

        public void OnRequest()
        {
            comms.NumRequests++;
        }

        public void OnSuccess()
        {
            comms.NumResponses++;
            comms.NumOKs++;
            comms.ErrorMessage = string.Empty;
        }

        public void OnError(Exception ex)
        {
            comms.NumResponses++;
            comms.NumErrors++;
            comms.ErrorMessage = MapError(ex);
        }

        public ConnectionDiagnostics Snapshot()
            => comms;

        private string MapError(Exception ex)
        {
            return ex switch
            {
                SlaveException se => $"MODBUS error {se.SlaveExceptionCode}",
                IOException => "Connection failure",
                SocketException => "Connection failure",
                TimeoutException => "Timeout failure",
                _ => $"Unknown error: {ex.Message}",
            };
        }
    }
}
