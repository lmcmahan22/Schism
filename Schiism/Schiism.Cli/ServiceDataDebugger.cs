// See https://aka.ms/new-console-template for more information
using Schiism.Cli.IPC;
using Schiism.Core.Enums;
using Schiism.Core.Models.IPC;
using Schiism.Core.Models.IPC.DTOs.Commands;
using Schiism.Core.Models.IPC.DTOs.Streams;


// To switch between the two debuggers, set the Startup object in project properties to either ServiceControlDebugger or ServiceDataDebugger

// Right - click the project
// Properties
// Go to Application
// Set Startup object
// Choose:
//  ServiceControlDebugger
//  or ServiceDataDebugger

public class ServiceDataDebugger
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
        var modbusDataSubscriber =
            new FEStreamSubscriber<ModbusData>(
                PipeConstants.ModbusDataStreamName);

        var connSettSubscriber =
            new FEStreamSubscriber<ConnectionDiagnostics>(
                PipeConstants.ConnDiagStreamName);

        // Commands
        var settingsCommandSender =
            new FECommandSender<SettingsConfig>(
                PipeConstants.SettingsCommandName);

        var initSettingsCommandReceiver =
            new FECommandReceiver<SettingsConfig>(
                PipeConstants.InitSettingsCommandName);

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

        // Data loop
        while (!cts.Token.IsCancellationRequested)
        {
            var completed = await Task.WhenAny(
                modbusTask,
                connTask);

            // Subscription died if one of these two was the completed task
            if (completed == modbusTask ||
                completed == connTask)
            {
                throw new Exception(
                    "IPC subscriptions disconnected.");
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
        string data = "Data: " + string.Join(", ", msg.Data);
        Console.WriteLine(data);
        //for (int i = 0; i < msg.Data.Count; i++)
        //{
        //    string.Concat($"[{i}] = {msg.Data[i]}");
        //}

        return Task.CompletedTask;
    }

    private static Task HandleConnDiagAsync(ConnectionDiagnostics msg)
    {
        Console.WriteLine($"Diagnostics: {msg}");
        return Task.CompletedTask;
    }

    private static Task HandleInitSettCommandAsync(SettingsConfig cfg)
    {
        // Acquire initial data
        ipAddress = cfg.IPAddress ?? ipAddress;
        dataLength = cfg.DataLength ?? dataLength;
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
}