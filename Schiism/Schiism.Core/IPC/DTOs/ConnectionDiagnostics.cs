// <copyright file="ConnectionDiagnostics.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.DTOs
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// ConnectionDiagnostics Record (immutable) to represent the diagnostics received from the Windows Service.
    /// </summary>
    public record ConnectionDiagnostics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDiagnostics"/> class. Note that the parameters must match the record properties for JSON deserialization to work correctly (case-insenstitive).
        /// </summary>
        /// <param name="numRequests">The number of requests sent from the Engine to the Modbus Server.</param>
        /// <param name="numResponses">The number of responses received by the Engine from the Modbus Server.</param>
        /// <param name="numOKs">The number of OK (successful) responses received.</param>
        /// <param name="numErrors">The number of Error (failed) responses received.</param>
        /// <param name="errorMessage">The error message associated with the last Error response received.</param>
        /// <param name="isConnected">The connection status of the Worker Service client app to the Modbus server.</param>
        /// <param name="timestamp">The timestamp of the diagnostics object, either at request time or response time.</param>
        [JsonConstructor]
        public ConnectionDiagnostics(int numRequests, int numResponses, int numOKs, int numErrors, string errorMessage, bool isConnected, DateTime timestamp)
        {
            NumRequests = numRequests;
            NumResponses = numResponses;
            NumOKs = numOKs;
            NumErrors = numErrors;
            ErrorMessage = errorMessage;
            IsConnected = isConnected;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets number of requests made.
        /// </summary>
        public int NumRequests { get; init; }

        /// <summary>
        /// Gets number of responses received.
        /// </summary>
        public int NumResponses { get; init; }

        /// <summary>
        /// Gets number of OK responses received.
        /// </summary>
        public int NumOKs { get; init; }

        /// <summary>
        /// Gets number of Error responses received.
        /// </summary>
        public int NumErrors { get; init; }

        /// <summary>
        /// Gets the Error Message.
        /// </summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Gets a value indicating whether the Client has a connection to the Server or not.
        /// </summary>
        public bool IsConnected { get; init; }

        /// <summary>
        /// Gets the timestamp of the diagnostics.
        /// </summary>
        public DateTime Timestamp { get; init; }
    }
}
