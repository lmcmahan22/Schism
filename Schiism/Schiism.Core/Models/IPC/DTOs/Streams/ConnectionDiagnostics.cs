using System.Text.Json.Serialization;

namespace Schiism.Core.Models.IPC.DTOs.Streams
{
    public record ConnectionDiagnostics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDiagnostics"/> class. Note that the parameters must match the record properties for JSON deserialization to work correctly (case-insenstitive).
        /// </summary>
        /// <param name="numRequests"></param>
        /// <param name="numResponses"></param>
        /// <param name="numOKs"></param>
        /// <param name="numErrors"></param>
        /// <param name="errorMessage"></param>
        /// <param name="isConnected"></param>
        /// <param name="timestamp"></param>
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
        /// Gets or Sets number of requests made.
        /// </summary>
        public int NumRequests { get; init; }

        /// <summary>
        /// Gets or Sets number of responses received.
        /// </summary>
        public int NumResponses { get; init; }

        /// <summary>
        /// Gets or Sets number of OK responses received.
        /// </summary>
        public int NumOKs { get; init; }

        /// <summary>
        /// Gets or Sets number of Error responses received.
        /// </summary>
        public int NumErrors { get; init; }

        /// <summary>
        /// Gets or Sets the Error Message.
        /// </summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Gets or Sets a value indicating whether the Client has a connection to the Server or not.
        /// </summary>
        public bool IsConnected { get; init; }

        /// <summary>
        /// Gets or Sets the timestamp of the diagnostics.
        /// </summary>
        public DateTime Timestamp { get; init; }
    }
}
