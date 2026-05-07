using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Models.DTOs.IPC_Records.Streams
{
    public record ConnectionDiagnostics
    {

        public ConnectionDiagnostics()
        {
            NumRequests = 0;
            NumResponses = 0;
            NumOKs = 0;
            NumErrors = 0;
            ErrorMessage = string.Empty;
            IsConnected = false;
            Timestamp = DateTime.UtcNow;
        }

        //public ConnectionDiagnostics(int numRequests, int numResponses, int numOKs, int numErrors, string errorMessage, bool isConnected, DateTime time)
        //{
        //    NumRequests = numRequests;
        //    NumResponses = numResponses;
        //    NumOKs = numOKs;
        //    NumErrors = numErrors;
        //    ErrorMessage = errorMessage;
        //    IsConnected = isConnected;
        //    Timestamp = time;
        //}

        /// <summary>
        /// Gets or Sets number of requests made.
        /// </summary>
        public int NumRequests { get; set; }

        /// <summary>
        /// Gets or Sets number of responses received.
        /// </summary>
        public int NumResponses { get; set; }

        /// <summary>
        /// Gets or Sets number of OK responses received.
        /// </summary>
        public int NumOKs { get; set; }

        /// <summary>
        /// Gets or Sets number of Error responses received.
        /// </summary>
        public int NumErrors { get; set; }

        /// <summary>
        /// Gets or Sets the Error Message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or Sets a value indicating whether the Client has a connection to the Server or not.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or Sets the timestamp of the diagnostics.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
