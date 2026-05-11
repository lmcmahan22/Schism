// See https://aka.ms/new-console-template for more information
using Schiism.Core.Models.DTOs.IPC_Records.Streams;
using Schiism.Service.Models.Implementations.IPC.Pipes.Streams;
using Schiism.Service.Models.Implementations.IPC;
using Microsoft.Extensions.Logging;
using Schiism.Service.Models.Implementations.IPC.Pipes.Commands;
using Schiism.Core.Models.DTOs.IPC_Records.Commands;
using Schiism.Core.Models.Enums;

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

    static SettingsConfig currentConfig = new SettingsConfig
    {
        IPAddress = "127.0.0.1",
        StartAddress = 0,
        DataLength = 10,
        DeviceId = 1,
        ScanRate = 1000,
        SelectedPollType = PollType.CoilStatus,
        SelectedDataSize = DataSize.Bit16,
        SelectedEndian = Endian.BigEndian,
        SelectedNumericBase = NumericBase.Decimal,
        TCPPort = 502,
        TCPTimeout = 2000,
        AsciiEnable = false
    };

    public static async Task Main(string[] args)
    {
        using ILoggerFactory loggerFactory =
            LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

        ILogger<StreamSubscriber<ModbusData>> modbusLogger =
            loggerFactory.CreateLogger<
                StreamSubscriber<ModbusData>>();

        ILogger<StreamSubscriber<ConnectionDiagnostics>> connLogger =
            loggerFactory.CreateLogger<
                StreamSubscriber<ConnectionDiagnostics>>();

        ILogger<CommandClient<SettingsConfig>> commandLogger =
            loggerFactory.CreateLogger<
                CommandClient<SettingsConfig>>();

        Console.WriteLine("Starting Service Debugger...");

        var modbusDataSubscriber = new StreamSubscriber<ModbusData>(PipeConstants.ModbusDataStreamName, modbusLogger);
        var connSettSubscriber = new StreamSubscriber<ConnectionDiagnostics>(PipeConstants.ConnDiagStreamName, connLogger);
        var settCommandPublisher = new CommandClient<SettingsConfig>(PipeConstants.SettingsCommandName, commandLogger);

        // Shared cancellation token for all operations, to allow for graceful shutdown.
        CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Starting subscribers...");
        _ = Task.Run(async () =>
        {
            try
            {
                await modbusDataSubscriber.SubscribeAsync(HandleModbusAsync, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ModbusData stream subscription start failure");
                Console.WriteLine(ex);
            }
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await connSettSubscriber.SubscribeAsync(HandleConnDiagAsync, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CommsDiag stream subscription start failure");
                Console.WriteLine(ex);
            }
        });

        while (!cts.IsCancellationRequested)
        {
            Console.Write("> ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0].ToLower())
            {
                case "show":
                    ShowConfig(currentConfig);
                    break;

                case "set":
                    await HandleSetCommand(parts, currentConfig, settCommandPublisher, cts);
                    break;

                case "quit":
                    cts.Cancel();
                    break;
            }
        }
        
        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    private static async Task HandleSetCommand(string[] parts, SettingsConfig cfg, CommandClient<SettingsConfig> settCommandPublisher, CancellationTokenSource cts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: set <field> <value>");
            Console.WriteLine("Fields: scanrate, datalength, ip, port, timeout, deviceid, datasize, endian, numericbase, polltype, ascii");
            return;
        }

        var field = parts[1].ToLower();
        var value = parts[2];

        switch (field)
        {
            case "deviceid":
                cfg.DeviceId = byte.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "ip":
                cfg.IPAddress = value;
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "port":
                cfg.TCPPort = ushort.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "timeout":
                cfg.TCPTimeout = int.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "scanrate":
                cfg.ScanRate = int.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "datalength":
                cfg.DataLength = byte.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "startaddress":
                cfg.StartAddress = ushort.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "datasize":
                cfg.SelectedDataSize = Enum.Parse<DataSize>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "endian":
                cfg.SelectedEndian = Enum.Parse<Endian>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "numericbase":
                cfg.SelectedNumericBase = Enum.Parse<NumericBase>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "polltype":
                cfg.SelectedPollType = Enum.Parse<PollType>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "ascii":
                cfg.AsciiEnable = bool.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            default:
                Console.WriteLine($"Unknown field: {field}");
                break;
        }

        try
        {
            await settCommandPublisher.SendAsync(currentConfig, cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("SettComms command client failure");
            Console.WriteLine(ex);
        }
    }

    private static void ShowConfig(SettingsConfig cfg)
    {
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"DeviceId      : {cfg.DeviceId}");
        Console.WriteLine($"IPAddress     : {cfg.IPAddress}");
        Console.WriteLine($"Port          : {cfg.TCPPort}");
        Console.WriteLine($"TCPTimeoutMs  : {cfg.TCPTimeout}");
        Console.WriteLine($"StartAddress  : {cfg.StartAddress}");
        Console.WriteLine($"ScanRateMs    : {cfg.ScanRate}");
        Console.WriteLine($"DataLength    : {cfg.DataLength}");
        Console.WriteLine($"DataSize      : {cfg.SelectedDataSize}");
        Console.WriteLine($"Endian        : {cfg.SelectedEndian}");
        Console.WriteLine($"NumericBase   : {cfg.SelectedNumericBase}");
        Console.WriteLine($"PollType      : {cfg.SelectedPollType}");
        Console.WriteLine($"ASCII         : {cfg.AsciiEnable}");
        Console.WriteLine("--------------------------------");
    }

    private static Task HandleModbusAsync(ModbusData msg)
    {
        // Console.WriteLine($"Data: {msg}");
        return Task.CompletedTask;
    }

    private static Task HandleConnDiagAsync(ConnectionDiagnostics msg)
    {
        // Console.WriteLine($"Diagnostics: {msg}");
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