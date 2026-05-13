// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;
using Schiism.Cli.IPC;
using Schiism.Core.Enums;
using Schiism.Core.Models.IPC;
using Schiism.Core.Models.IPC.DTOs.Commands;
using Schiism.Core.Models.IPC.DTOs.Streams;
using System.Security.Principal;

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
    // Local variables to hold current config values for display and command sending. Initialized with defaults matching the Service's default config.
    private static string ipAddress = "127.0.0.1";
    private static ushort startAddress = 0;
    private static byte dataLength = 10;
    private static byte deviceId = 1;
    private static int scanRate = 1000;
    private static PollType selectedPollType = PollType.CoilStatus;
    private static DataSize selectedDataSize = DataSize.Bit16;
    private static Endian selectedEndian = Endian.BigEndian;
    private static NumericBase selectedNumericBase = NumericBase.Decimal;
    private static ushort tCPPort = 1502;
    private static int tCPTimeout = 2000;
    private static bool asciiEnable = false;

    public static async Task Main(string[] args)
    {
        // var identity = WindowsIdentity.GetCurrent();
        // var principal = new WindowsPrincipal(identity);

        // Console.WriteLine(
        //    principal.IsInRole(WindowsBuiltInRole.Administrator)
        //        ? "Running elevated"
        //        : "Not elevated");

        Console.WriteLine("Starting Service Debugger...");

        var modbusDataSubscriber = new StreamSubscriber<ModbusData>(PipeConstants.ModbusDataStreamName);
        var connSettSubscriber = new StreamSubscriber<ConnectionDiagnostics>(PipeConstants.ConnDiagStreamName);
        var settCommandPublisher = new CommandClient<SettingsConfig>(PipeConstants.SettingsCommandName);

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
                    ShowConfig();
                    break;

                case "set":
                    await HandleSetCommand(parts, settCommandPublisher, cts);
                    break;

                case "quit":
                    cts.Cancel();
                    break;
            }
        }
        
        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    private static async Task HandleSetCommand(string[] parts, CommandClient<SettingsConfig> settCommandPublisher, CancellationTokenSource cts)
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
                deviceId = byte.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "ip":
                ipAddress = value;
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "port":
                tCPPort = ushort.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "timeout":
                tCPTimeout = int.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "scanrate":
                scanRate = int.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "datalength":
                dataLength = byte.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "startaddress":
                startAddress = ushort.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "datasize":
                selectedDataSize = Enum.Parse<DataSize>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "endian":
                selectedEndian = Enum.Parse<Endian>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "numericbase":
                selectedNumericBase = Enum.Parse<NumericBase>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "polltype":
                selectedPollType = Enum.Parse<PollType>(value, true);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            case "ascii":
                asciiEnable = bool.Parse(value);
                Console.WriteLine($"Updated {field} to {value}");
                break;

            default:
                Console.WriteLine($"Unknown field: {field}");
                break;
        }

        try
        {
            SettingsConfig cfg = new SettingsConfig(
                ipAddress, dataLength, startAddress,
                tCPPort, scanRate, tCPTimeout,
                deviceId, selectedDataSize, selectedPollType,
                asciiEnable, selectedNumericBase, selectedEndian);
            await settCommandPublisher.SendAsync(cfg, cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("SettComms command client failure");
            Console.WriteLine(ex);
        }
    }

    private static void ShowConfig()
    {
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"DeviceId      : {deviceId}");
        Console.WriteLine($"IPAddress     : {ipAddress}");
        Console.WriteLine($"Port          : {tCPPort}");
        Console.WriteLine($"TCPTimeoutMs  : {tCPTimeout}");
        Console.WriteLine($"StartAddress  : {startAddress}");
        Console.WriteLine($"ScanRateMs    : {scanRate}");
        Console.WriteLine($"DataLength    : {dataLength}");
        Console.WriteLine($"DataSize      : {selectedDataSize}");
        Console.WriteLine($"Endian        : {selectedEndian}");
        Console.WriteLine($"NumericBase   : {selectedNumericBase}");
        Console.WriteLine($"PollType      : {selectedPollType}");
        Console.WriteLine($"ASCII         : {asciiEnable}");
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