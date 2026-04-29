using Microsoft.Extensions.Hosting.WindowsServices;
using Schiism.Core;
using Schiism.Core.Abstractions;
using Schiism.Core.Models.Clients;
using Schiism.Core.Models.Handlers;
using Schiism.Service;
using Schiism.Service.FileLogging;
using Schiism.Service.Publishers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// Service Name (defined once here so we don't accidentally mistype it anywhere)
const string ServiceName = "SchiismModbusClientService";
bool useEventViewer = false; // personal use, so I can switch back to EventViewer, if desired

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

    // If we have installed the app via the batch script, then don't continue down to the Run sequence! That will be handled later!
    return;
}

// Creates the host with: DI container, Logging, and Configuration capabilities
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Add the Windows Service for WS mode only
// Note that the program can identify a Windows Service from a console execution through VSStudio depending on how you start the app
bool isService = WindowsServiceHelpers.IsWindowsService();
// Batch script or "sc.exe start SchiismModbusClientService" in command line
if (isService)
{
    // Service/IPC Use
    builder.Services.AddWindowsService();
    builder.Logging.ClearProviders();

    ConfigureService(true); // This will eventually be controlled by the UI instead, via a WPF checkbox! The boolean controls startup behavior only, recovery behavior is non-configurable atm.

    if (useEventViewer)
    {
        // I attempted to add filtering via C# to get Information level messages to appear in EventViewer, each containing the MODBUS data, but that didn't work.
        // The only solution was to specify that I wanted Information level event to appear via the appsettings.json files. Annoying!
        // builder.Logging.SetMinimumLevel(LogLevel.Trace);

    //    "Logging": {
    //        "LogLevel": {
    //            "Default": "Information",
    //  "Microsoft.Hosting.Lifetime": "Information"
    //        },
    //"EventLog": {
    //            "LogLevel": {
    //                "Default": "Information"
    //            }
    //        }
    //    },
            // Define the EventLog: What the source should be called, and where the log should appear

        // Move this to a file eventually, for easier user interaction!
        builder.Logging.AddEventLog(settings =>
        {
            settings.SourceName = $"{ServiceName}";
            settings.LogName = "Application";
        });
    }
    else
    {
        // make available for IPC AND print to log file!
        builder.Logging.AddProvider(new FileLoggerProvider($"C:\\Users\\lmcmahan\\OneDrive - Precision Valve and Automation\\Desktop\\SchiismLogs"));
    }

    // Data publisher (currently pushes poll data to event viewer)
    builder.Services.AddSingleton<IDataPublisher, IPCDataPublisher>();
}
// ".\Schiism.Service.exe" in command line
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

// This currently runs at startup everytime. A bit redundant, especially since AutoStart is configurable, and recovery behavior only needs to be configured on installation. Spend some more time with this when you can.
static void ConfigureService(bool enableAutoStart)
{
    // "delayed-auto" also works in place of "auto" here, but took about 90 seconds longer on my desktop PC to start up. Keeping this as "auto" until I see reason to change it.
    string startType = enableAutoStart ? "auto" : "demand";

    RunSc($"config {ServiceName} start= {startType}");
    RunSc($"failureflag {ServiceName} 1");
    RunSc($"failure {ServiceName} reset= 0 actions= restart/5000/restart/5000/restart/5000");
}

static void RunSc(string arguments)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = arguments,
        Verb = "runas", // requires admin
        CreateNoWindow = true,
        UseShellExecute = true
    });
}

// For triggering a crash, enter the following in terminal from any file location
// taskkill /F /IM Schiism.Service.exe

// Command for telling the scm to auto restart the app, also a checkbox in WPF:
// sc.exe failure SchiismModbusClientService reset= 0 actions= restart/5000/restart/5000/restart/5000