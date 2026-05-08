// See https://aka.ms/new-console-template for more information
using Schiism.Core.Models.DTOs.IPC_Records.Streams;
using Schiism.Service.Models.Implementations.IPC.Pipes.Streams;
using Schiism.Service.Models.Implementations.IPC;
using Microsoft.Extensions.Logging;

//1. Build tiny IPC console client
//2. Fully validate transport/lifecycle
//3. Harden reconnect behavior
//4. THEN integrate WPF

// Console app should validate working Service and IPC connectivity
// Test the following:

//Command Flow

//Test:

//restart engine
//connect/disconnect modbus
//configuration updates
//ping/pong

//Verify:

//responses
//logging
//exception handling

//Streaming Data

//Verify:

//telemetry arrives
//no deadlocks
//no blocking
//timing acceptable

//Disconnect Handling

//Test:

//client exits abruptly
//forced pipe close
//repeated reconnects

//Verify:

//service survives
//client cleanup works
//no stale references

// Use Pipe names from .Core PipeConstants!

public class ServiceDebugger
{
    public static async Task Main(string[] args)
    {
        using ILoggerFactory loggerFactory =
            LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

        ILogger<NamedPipeStreamSubscriber<ModbusData>> modbusLogger =
            loggerFactory.CreateLogger<
                NamedPipeStreamSubscriber<ModbusData>>();

        ILogger<NamedPipeStreamSubscriber<ConnectionDiagnostics>> connLogger =
            loggerFactory.CreateLogger<
                NamedPipeStreamSubscriber<ConnectionDiagnostics>>();

        Console.WriteLine("Starting Service Debugger...");

        var modbusDataSubscriber = new NamedPipeStreamSubscriber<ModbusData>(PipeConstants.ModbusDataStreamName, modbusLogger);
        var connSettSubscriber = new NamedPipeStreamSubscriber<ConnectionDiagnostics>(PipeConstants.ConnDiagStreamName, connLogger);
        // var settCommandPublisher = new NamedPipeCommandClient<SettingsConfig>(PipeConstants.SettingsCommandName);

        // Shared cancellation token for all operations, to allow for graceful shutdown.
        CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Starting subscribers...");
        Task modbusDataTask = modbusDataSubscriber.StartAsync(HandleModbusAsync, cts.Token);
        Task connSettTask = connSettSubscriber.StartAsync(HandleConnDiagAsync, cts.Token);
        await Task.WhenAll(modbusDataTask, connSettTask);
    }

    static Task HandleModbusAsync(ModbusData msg)
    {
        Console.WriteLine($"Data: {msg}");
        return Task.CompletedTask;
    }

    static Task HandleConnDiagAsync(ConnectionDiagnostics msg)
    {
        Console.WriteLine($"Diagnostics: {msg}");
        return Task.CompletedTask;
    }
}
//Then add:

//StreamReader
//StreamWriter
//async receive loop

// Testing in a console isolates IPC related bugs to the Service. This additionally allows you to find Service functionality bugs sooner.

// Consider a command shell, where you can simply enter commands in order to test the Service in isolation! Production systems often keep these permanantly, just for future use!
//> ping
//Pong

//> restart
//Restart acknowledged

//> status
//Connected: True
//Polling: True

//Once basic IPC works:

//Simulate failure conditions
//Kill service mid-stream

//Verify reconnect logic.

//Spam commands rapidly

//Look for:

//race conditions
//deadlocks
//pipe corruption
//Disconnect during publish

//Validate cleanup.