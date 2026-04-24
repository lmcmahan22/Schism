// <copyright file="ModbusClientConsoleApp.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using Schiism.Core;
using Schiism.Core.Abstractions;
using Schiism.Core.Models;
using Schiism.Core.Models.Clients;
using Schiism.Core.Models.Config;
using Schiism.Core.Models.Enums;
using Schiism.Core.Models.Publishers;

// Once you get this running:
// Connect error messages to an event so you can see these in the console
// Add command-line args (IP, port, etc.)
// Swap ConsoleDataPublisher with IPC publisher
// Reuse the same Core in your WPF app

// NOTE: Your console app is able to refer to the .Core project, because you configured a reference in the solution explorer! Projects can't see each other without that step!
// Define ModbusConfig parameters here (eventually, the UI will bring this info forward to the core/engine instead
ModbusConfig config = new ModbusConfig
{
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
IEnginePublisher publisher = new ConsolePublisher();

// Engine
ModbusEngineCore engine = new ModbusEngineCore(client, interpreter, publisher);

// Cancellation handling
CancellationTokenSource cts = new CancellationTokenSource();

// Ctrl+C to stop
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

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
