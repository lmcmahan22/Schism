namespace Schiism.Core.Abstractions.Modbus
{
    public interface ICommsMetrics
    {
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
    }
}
