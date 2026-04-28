namespace Schiism.Service
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Handlers;

    // Contains the background execution loop that will work with your .Core project.
    public class Worker : BackgroundService
    {
        // Introduce your .Core engine
        private readonly IEngineService _engine;
        private readonly ModbusConfig _config;

        // Logger object
        private readonly ILogger<Worker> _logger;

        // Constructor
        public Worker(IEngineService engine, IConfiguration config, ILogger<Worker> logger)
        {
            _engine = engine;
            _config = config.GetSection("Modbus").Get<ModbusConfig>()
                ?? throw new InvalidOperationException("Missing Modbus config");
            _logger = logger;
        }

        // Background loop (continues until shutdown or cancellation token (ex. CTRL+C) is received)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Configure the engine with your established configuration
            _engine.Configure(_config);

            // Begin the engine's execution in the background
            await _engine.StartAsync(stoppingToken);

            // Keeps the ExecuteAsync method alive indefinitely until cancellation.
            await Task.Delay(Timeout.Infinite, stoppingToken);
            
            // Stop the engine if the cancellation token was received
            await _engine.StopAsync();
        }
    }
}
