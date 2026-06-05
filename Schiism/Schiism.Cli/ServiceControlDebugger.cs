// <copyright file="ServiceControlDebugger.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.Threading.Channels;
using Schiism.Cli.IPC;
using Schiism.Core.Common;
using Schiism.Core.Configuration.Enums;
using Schiism.Core.IPC.DTOs.Commands;
using Schiism.Core.IPC.DTOs.Streams;


// To switch between the two debuggers, set the Startup object in project properties to either ServiceControlDebugger or ServiceDataDebugger

// Right - click the project
// Properties
// Go to Application
// Set Startup object
// Choose:
//  ServiceControlDebugger
//  or ServiceDataDebugger

/// <summary>
/// Console based Service Control Debugger (sister program to Service Data Debugger).
/// </summary>
public class ServiceControlDebugger
{
    private static string ipAddress = "127.0.0.1";
    private static ushort startAddress = 0;
    private static byte deviceId = 1;
    private static int scanRate = 1000;
    private static PollType selectedPollType = PollType.CoilStatus;
    private static DataSize selectedDataSize = DataSize.Bit16;
    private static Endian selectedEndian = Endian.BigEndian;
    private static NumericBase selectedNumericBase = NumericBase.Decimal;
    private static ushort tCPPort = 1502;
    private static int tCPTimeout = 2000;
    private static bool asciiEnable = false;

    /// <summary>
    /// Service Control Debugger program. Sister program to the ServiceDataDebugger program.
    /// </summary>
    /// <param name="args">Should be empty.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        using CancellationTokenSource cts = new();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Starting Service Debugger...");

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(cts);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"IPC session lost: {ex.Message}");

                Console.WriteLine(
                    "Waiting for backend recovery...");

                await Task.Delay(2000, cts.Token);
            }
        }
    }

    private static async Task RunSessionAsync(
    CancellationTokenSource cts)
    {
        // Streams
        FEStreamSubscriber<ModbusData> modbusDataSubscriber =
            new FEStreamSubscriber<ModbusData>(
                NamingConstants.ModbusDataStreamName);

        FEStreamSubscriber<ConnectionDiagnostics> connSettSubscriber =
            new FEStreamSubscriber<ConnectionDiagnostics>(
                NamingConstants.ConnDiagStreamName);

        // Commands
        FECommandSender settingsCommandSender =
            new FECommandSender(
                NamingConstants.SettingsCommandName);

        FECommandReceiver initSettingsCommandReceiver =
            new FECommandReceiver(
                NamingConstants.InitSettingsCommandName);

        Console.WriteLine(
            "Waiting for initialization settings...");

        await initSettingsCommandReceiver.ReceiveAsync(
            HandleInitSettCommandAsync,
            cts.Token);

        Console.WriteLine("Connected to backend.");

        // Start subscriptions
        Task modbusTask =
            modbusDataSubscriber.SubscribeAsync(
                HandleModbusAsync,
                cts.Token);

        Task connTask =
            connSettSubscriber.SubscribeAsync(
                HandleConnDiagAsync,
                cts.Token);

        Channel<string> inputChannel = Channel.CreateUnbounded<string>();

        Task inputTask = StartConsoleInputAsync(
            inputChannel.Writer,
            cts.Token);

        TaskCompletionSource quitTcs = new TaskCompletionSource();

        // Input loop
        while (!cts.Token.IsCancellationRequested)
        {
            Console.Write("> ");

            Task<string> inputReadTask = inputChannel.Reader.ReadAsync(cts.Token).AsTask();

            Task completed = await Task.WhenAny(
                inputReadTask,
                quitTcs.Task,
                modbusTask,
                connTask);

            // quit if this task completed
            if (completed == quitTcs.Task)
            {
                cts.Cancel();
                return;
            }

            // Subscription died if one of these two was the completed task
            if (completed == modbusTask ||
                completed == connTask)
            {
                throw new Exception(
                    "IPC subscriptions disconnected.");
            }

            // Input received
            string input = await inputReadTask;

            string[] parts = input.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0].ToLower())
            {
                case "show":
                    ShowConfig();
                    break;

                case "set":
                    await HandleSetCommand(
                        parts,
                        settingsCommandSender,
                        cts);
                    break;

                case "quit":
                    quitTcs.TrySetResult();
                    cts.Cancel();
                    return;
            }

            // Detect subscription failure
            if (modbusTask.IsFaulted ||
                connTask.IsFaulted ||
                modbusTask.IsCompleted ||
                connTask.IsCompleted)
            {
                throw new Exception(
                    "Subscription connection lost.");
            }
        }
    }

    private static async Task HandleSetCommand(string[] parts, FECommandSender settCommandSender, CancellationTokenSource cts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: set <field> <value>");
            Console.WriteLine("Fields: scanrate, datalength, ip, port, timeout, deviceid, datasize, endian, numericbase, polltype, ascii");
            return;
        }

        string field = parts[1].ToLower();
        string value = parts[2];

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
                ipAddress,
                null,
                startAddress,
                tCPPort,
                scanRate,
                tCPTimeout,
                deviceId,
                selectedDataSize,
                selectedPollType,
                asciiEnable,
                selectedNumericBase,
                selectedEndian,
                null,
                null);

            Console.WriteLine("Sending settings command");
            await settCommandSender.SendAsync(cfg, cts.Token);
            Console.WriteLine("Settings command sent");
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

    private static Task HandleInitSettCommandAsync(SettingsConfig cfg)
    {
        // Acquire initial data
        ipAddress = cfg.IPAddress ?? ipAddress;
        startAddress = cfg.StartAddress ?? startAddress;
        tCPPort = cfg.TCPPort ?? tCPPort;
        scanRate = cfg.ScanRate ?? scanRate;
        tCPTimeout = cfg.TCPTimeout ?? tCPTimeout;
        deviceId = cfg.DeviceId ?? deviceId;
        selectedDataSize = cfg.SelectedDataSize ?? selectedDataSize;
        selectedPollType = cfg.SelectedPollType ?? selectedPollType;
        asciiEnable = cfg.AsciiEnable ?? asciiEnable;
        selectedNumericBase = cfg.SelectedNumericBase ?? selectedNumericBase;
        selectedEndian = cfg.SelectedEndian ?? selectedEndian;

        Console.WriteLine("Received initial settings:");
        ShowConfig();

        return Task.CompletedTask;
    }

    private static Task StartConsoleInputAsync(
    ChannelWriter<string> writer,
    CancellationToken ct)
    {
        return Task.Run(
            async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                string? line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                await writer.WriteAsync(line, ct);
            }
        },
            ct);
    }
}