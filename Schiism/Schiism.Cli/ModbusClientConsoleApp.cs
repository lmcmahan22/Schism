// <copyright file="ModbusClientConsoleApp.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

// Once you get this running:
// Swap ConsoleDataPublisher with IPC publisher
// Reuse the same Core in your WPF app

// NOTE: Your console app is able to refer to the .Core project, because you configured a reference in the solution explorer! Projects can't see each other without that step!
// Define ModbusConfig parameters here (eventually, the UI will bring this info forward to the core/engine instead
namespace Sciism.Cli
{
    using System.Threading.Tasks;
    using Schiism.Core;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Clients;
    using Schiism.Core.Models.Config;
    using Schiism.Core.Models.Enums;
    using Schiism.Core.Models.Handlers;
    using Schiism.Core.Models.Publishers;

    public class ModbusClientConsoleApp
    {
        public static async Task Main(string[] args)
        {
            ModbusConfig config = new ModbusConfig
            {
                // Default parameters
                IPAddress = "192.168.100.20",
                TCPPort = 502,
                DeviceId = 1,
                StartAddress = 0,
                DataLength = 10,
                ScanRate = 1000,
                TCPTimeout = 2000,
                SelectedPollType = PollType.CoilStatus,
                SelectedDataSize = DataSize.Bit16,
                SelectedNumericBase = NumericBase.Decimal,
                SelectedEndian = Endian.BigEndian,
                AsciiEnable = false,
            };

            // Dependencies
            IModbusClient client = new NModbusClient();
            ModbusInterpreter interpreter = new ModbusInterpreter();
            IDataPublisher publisher = new ConsoleDataPublisher();
            IEngineDiagnostics diag = new ConsoleDiagPublisher();

            // Engine
            ModbusEngineCore engine = new ModbusEngineCore(client, interpreter, publisher, diag);

            // Cancellation handling
            CancellationTokenSource cts = new CancellationTokenSource();

            // Parameter review (these can be modified by right clicking the .Cli project --> Debug --> General --> Open debug launch profiles UI. Here, you can type in your arguments in command line format (one on each line acceptable)
            parameterReview(config, args);

            // Ctrl+C to stop
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Parameter display
            printConfigData(config);

            Console.WriteLine("Starting Modbus polling... Press Ctrl+C to stop.");

            // Run engine
            await engine.RunAsync(config, cts.Token);
            Console.WriteLine("Loop started");

            // Report connectcion status
            engine.ConnectionChanged += connected =>
            {
                Console.WriteLine(connected ? "Connected" : "Disconnected");
            };

            Console.WriteLine("Stopped.");
        }

        private static void parameterReview(ModbusConfig config, string[] args)
        {
            // Argument parsing to overwrite defaults, if present:
            var parsed = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-") && i + 1 < args.Length)
                {
                    parsed[args[i]] = args[i + 1];
                    i++;
                }
            }

            if (parsed.TryGetValue("-ip", out var ip))
            {
                config.IPAddress = ip;
            }

            if (parsed.TryGetValue("-port", out var tcpPort))
            {
                config.TCPPort = Convert.ToUInt16(tcpPort);
            }

            if (parsed.TryGetValue("-deviceID", out var devID))
            {
                config.DeviceId = Convert.ToByte(devID);
            }

            if (parsed.TryGetValue("-startAddress", out var startAdd))
            {
                config.StartAddress = Convert.ToUInt16(startAdd);
            }

            if (parsed.TryGetValue("-length", out var dataLength))
            {
                config.DataLength = Convert.ToByte(dataLength);
            }

            if (parsed.TryGetValue("-scanRate", out var scnRt))
            {
                config.ScanRate = Convert.ToInt32(scnRt);
            }

            if (parsed.TryGetValue("-tcpTimeout", out var tcpTO))
            {
                config.TCPTimeout = Convert.ToInt32(tcpTO);
            }

            if (parsed.TryGetValue("-pollType", out var selPollTyp))
            {
                config.SelectedPollType = selPollTyp switch
                {
                    "is" => PollType.InputStatus,
                    "hr" => PollType.HoldingRegisters,
                    "ir" => PollType.InputRegisters,
                    _ => PollType.CoilStatus,
                };
            }

            if (parsed.TryGetValue("-dataSize", out var selDatSiz))
            {
                config.SelectedDataSize = selDatSiz switch
                {
                    "32" => DataSize.Bit32,
                    "64" => DataSize.Bit64,
                    _ => DataSize.Bit16,
                };
            }

            if (parsed.TryGetValue("-numericBase", out var selNumBas))
            {
                config.SelectedNumericBase = selNumBas switch
                {
                    "Int" => NumericBase.Integer,
                    "Hex" => NumericBase.Hexadecimal,
                    "Bin" => NumericBase.Binary,
                    "Flo" => NumericBase.Float,
                    _ => NumericBase.Decimal,
                };
            }

            if (parsed.TryGetValue("-endian", out var selEndian))
            {
                config.SelectedEndian = selEndian switch
                {
                    "LE" => Endian.LittleEndian,
                    "BEsw" => Endian.BigEndianSW,
                    "LEsw" => Endian.LittleEndianSW,
                    _ => Endian.BigEndian,
                };
            }

            if (parsed.TryGetValue("-asciiEnable", out var asciien))
            {
                config.AsciiEnable = asciien switch
                {
                    "True" => true,
                    _ => false,
                };
            }
        }

        private static void printConfigData(ModbusConfig config)
        {
            // Print config data
            Console.WriteLine($"--------------------------------------------------------------");
            Console.WriteLine($"IP Address: {config.IPAddress}");
            Console.WriteLine($"TCP Port: {config.TCPPort}");
            Console.WriteLine($"Device ID: {config.DeviceId}");
            Console.WriteLine($"Starting Address: {config.StartAddress}");
            Console.WriteLine($"Data Length: {config.DataLength}");
            Console.WriteLine($"Scan Rate: {config.ScanRate}");
            Console.WriteLine($"TCP Timeout: {config.TCPTimeout}");
            Console.WriteLine($"Selected Poll Type: {config.SelectedPollType}");
            Console.WriteLine($"Selected Data Type: {config.SelectedDataSize}");
            Console.WriteLine($"Selected Numeric Base: {config.SelectedNumericBase}");
            Console.WriteLine($"Selected Endian: {config.SelectedEndian}");
            Console.WriteLine($"Ascii Display Enable: {config.AsciiEnable}");
            Console.WriteLine($"--------------------------------------------------------------");
        }
    }
}