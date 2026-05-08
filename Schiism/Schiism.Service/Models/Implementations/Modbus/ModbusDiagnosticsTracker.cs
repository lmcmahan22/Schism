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
        private int numRequests;
        private int numResponses;
        private int numOKs;
        private int numErrors;
        private string errorMessage;

        public void OnRequest()
        {
            this.numRequests++;
        }

        public void OnSuccess()
        {
            this.numResponses++;
            this.numOKs++;
            this.errorMessage = string.Empty;
        }

        public void OnError(Exception ex)
        {
            this.numResponses++;
            this.numErrors++;
            this.errorMessage = MapError(ex);
        }

        public ConnectionDiagnostics Snapshot()
        {
            return new ConnectionDiagnostics
            {
                Timestamp = DateTime.UtcNow,
                NumRequests = this.numRequests,
                NumResponses = this.numResponses,
                NumOKs = this.numOKs,
                NumErrors = this.numErrors,
                ErrorMessage = this.errorMessage,
            };
        }

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
