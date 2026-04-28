using Microsoft.Extensions.Hosting.WindowsServices;
using Schiism.Core;
using Schiism.Core.Abstractions;
using Schiism.Core.Models.Clients;
using Schiism.Core.Models.Handlers;
using Schiism.Service;
using Schiism.Service.Publishers;
using System.Diagnostics;

// Service Name (defined once here so we don't accidentally mistype it anywhere)
const string ServiceName = "SchiismModbusClientService";

// Installation parameter (used for batch script when publishing and starting the service)
if (args.FirstOrDefault()?.Trim().ToLowerInvariant() == "-install")
{
    // Acquire .exe path
    var exePath = Process.GetCurrentProcess().MainModule!.FileName!;

    // Stop the service if it's already running, then install this one
    // NOTE: Having the ServiceName inconsistent between calls made it so your logging and Worker execution didn't work as intended. Be careful with this!
    RunCommand("sc.exe", $"stop {ServiceName}");
    RunCommand("sc.exe", $"delete {ServiceName}");
    RunCommand("sc.exe", $"create {ServiceName} binPath= \"{exePath}\" start= auto");
    RunCommand("sc.exe", $"description {ServiceName} \"Schiism Modbus Client Engine\"");
    Console.WriteLine($"Installed {ServiceName} at: {exePath}");

    // If we have installed the app via the batch script, then don't continue down to the Run sequence! That will be handled later!
    return;
}

// Creates the host with: DI container, Logging, and Configuration capabilities
var builder = Host.CreateApplicationBuilder(args);

// Add the Windows Service for WS mode only
// Note that the program can identify a Windows Service from a console execution through VSStudio depending on how you start the app
bool isService = WindowsServiceHelpers.IsWindowsService();
if (isService)
{
    // Service/IPC Use
    builder.Services.AddWindowsService();
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(LogLevel.Trace);

    // Define the EventLog: What the source should be called, and where the log should appear
    // Move this to a file eventually, for easier user interaction!
    builder.Logging.AddEventLog(settings =>
    {
        settings.SourceName = $"{ServiceName}";
        settings.LogName = "Application";
    });

    // STILL NEEDS TO BE WRITTEN OUT!
    builder.Services.AddSingleton<IDataPublisher, IPCDataPublisher>();
}
else
{
    // Pure Console Use
    builder.Logging.AddConsole();
    builder.Services.AddSingleton<IDataPublisher, ConsoleDataPublisher>();
}

// Define Core Services (Builder.Services is a dependency container)
builder.Services.AddSingleton<IEngineService, ModbusEngineCore>();
builder.Services.AddSingleton<IModbusClient, NModbusClient>();
builder.Services.AddSingleton<IEngineLogger, EngineLogger>();
builder.Services.AddSingleton<IModbusInterpreter, ModbusInterpreter>();

// Add an instance of the Worker class as the hosted service
builder.Services.AddHostedService<Worker>();

// Build and run!
var host = builder.Build();
host.Run();

// Helper method for the above -install shortcut
static void RunCommand(string file, string args)
{
    var psi = new ProcessStartInfo(file, args)
    {
        Verb = "runas",
        CreateNoWindow = false,
        UseShellExecute = true
    };

    Process.Start(psi)?.WaitForExit();
}