namespace Schiism.Service
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Handlers;
    using System.Diagnostics.Metrics;
    using System.Text;

    // Contains the background execution loop that will work with your .Core project.
    // Formerly the "Worker" class
    public class Worker : BackgroundService
    {
        // Introduce your .Core engine
        private readonly IEngineService _engine;
        private readonly ModbusConfig _config;

        // Observe lifetime of host application to make sure we don't start polling until the app has completely started!
        private readonly IHostApplicationLifetime _lifetime;

        // Logger object
        private readonly ILogger<Worker> _logger;

        // Constructor
        public Worker(IEngineService engine, IConfiguration config, ILogger<Worker> logger, IHostApplicationLifetime lifetime)
        {
            _engine = engine;
            _config = config.GetSection("Modbus").Get<ModbusConfig>()
                ?? throw new InvalidOperationException("Missing Modbus config");
            _logger = logger;
            _lifetime = lifetime;
        }

        // Background loop (continues until shutdown or cancellation token (ex. CTRL+C) is received)
        // Host starts
        // BackgroundService.ExecuteAsync begins
        // Host logs "Application started"
        // Your loop runs inside ExecuteAsync
        // Logs appear AFTER startup
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _engine.Configure(_config);
            await _engine.RunAsync(stoppingToken);
        }
    }
}
