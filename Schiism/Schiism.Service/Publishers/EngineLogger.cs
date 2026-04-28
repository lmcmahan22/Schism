namespace Schiism.Service.Publishers
{
    using System;
    using Schiism.Core.Abstractions;

    public class EngineLogger : IEngineLogger
    {
        // Create the Windows Logger for this class
        private readonly ILogger<EngineLogger> _logger;

        public EngineLogger(ILogger<EngineLogger> logger)
        {
            _logger = logger;
        }

        // Implement in Engine if you need to observe/timestamp when expected events occur. Not incredibly necessary unless the you or the user need more transparency.
        public void Info(string message)
        {
            _logger.LogInformation(message);
        }

        // Similar to Error, but should execute if the system is still running (i.e. program didn't need to leave the try loop)
        public void Warning(string message)
        {
            _logger.LogWarning(message);
        }

        public void Error(string message, Exception ex)
        {
            _logger.LogError(message, ex);
        }
    }
}
