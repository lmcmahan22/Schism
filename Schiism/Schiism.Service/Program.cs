using Schiism.Core;
using Schiism.Core.Abstractions;
using Schiism.Core.Models.Clients;
using Schiism.Core.Models.Handlers;
using Schiism.Service;
using Schiism.Service.Publishers;

// Creates the host with: DI container, Logging (typically with Windows ILogger, but now with our own IEngineLogger interface), Configuration (appsettings.json, env vars, etc.)
var builder = Host.CreateApplicationBuilder(args);

// Detect Runtime Mode
// bool isConsole = args.Contains("--console") || Environment.UserInteractive;
// bool useIpc = args.Contains("--ipc");

// Add Logger via Dependency Injection (make sure you do this as Ryan suggested in your meeting)

// "The engine does not care what implementation is used
// It only knows the interface. That’s Dependency Inversion in practice."

// Logging Baseline
builder.Logging.ClearProviders();

// Future implementations:
//builder.Logging.AddFile(...);
//builder.Logging.AddEventLog();
//builder.Logging.AddDebug();

// Define Core Services (Builder.Services is a dependency container)
// Called in the Engine constructor
builder.Services.AddSingleton<IEngineService, ModbusEngineCore>();
builder.Services.AddSingleton<IModbusClient, NModbusClient>();
builder.Services.AddSingleton<IEngineLogger, EngineLogger>();
builder.Services.AddSingleton<IModbusInterpreter, ModbusInterpreter>();

// Console Use
builder.Logging.AddConsole();
builder.Services.AddSingleton<IDataPublisher, ConsoleDataPublisher>();

// IPC Use
// builder.Host.UseWindowsService();
// builder.Services.AddSingleton<IDataPublisher, IPCDataPublisher>();

builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
