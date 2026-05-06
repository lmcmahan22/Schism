// <copyright file="ModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations.Modbus
{
    using System.Linq;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.Enums;

    /// <inheritdoc/>
    public class ModbusClient : IModbusClient
    {

        private readonly SemaphoreSlim connectionLock = new(1, 1);
        private TcpClient? client;
        private IModbusMaster? master;

        public async Task ConnectAsync(IModbusConfig config)
        {
            await connectionLock.WaitAsync();
            try
            {
                if (client?.Connected == true)
                {
                    return;
                }

                client = CreateClient(config.IPAddress, config.TCPPort, config.TCPTimeout);

                // TcpClient constructor connects synchronously, so just wrap for consistency
                await Task.CompletedTask;

                master = CreateMaster(client, config.TCPTimeout);
            }
            finally
            {
                connectionLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await connectionLock.WaitAsync();
            try
            {
                master?.Dispose();
                master = null;

                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }
            }
            finally
            {
                connectionLock.Release();
            }
        }

        /// <inheritdoc/>
        public List<ushort> ReadData(IModbusConfig config)
        {
            if (master == null)
            {
                throw new InvalidOperationException("Modbus client not connected.");
            }

            return config.SelectedPollType switch
            {
                PollType.InputStatus =>
                    ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, true),

                PollType.HoldingRegisters =>
                    ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, false),

                PollType.InputRegisters =>
                    ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, true),

                _ =>
                    ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, false),
            };
        }

        private static List<ushort> ReadDigitals(
            IModbusMaster master,
            byte deviceId,
            ushort startAddress,
            ushort dataLength,
            bool isInputs)
        {
            bool[] rawData;
            rawData = isInputs ? master.ReadInputs(deviceId, startAddress, dataLength) : master.ReadCoils(deviceId, startAddress, dataLength);

            // Convert to ushorts, so bools can be displayed as 1s and 0s.
            // This also makes it so we can handle this data in a similar manner as register data, which returns as ushorts natively.
            return [.. rawData.Select(x => Convert.ToUInt16(x))];
        }

        private static List<ushort> ReadRegisters(
            IModbusMaster master,
            byte deviceId,
            ushort startAddress,
            ushort dataLength,
            bool isInputs)
        {
            ushort[] rawData;
            rawData = isInputs ? master.ReadInputRegisters(deviceId, startAddress, dataLength) : master.ReadHoldingRegisters(deviceId, startAddress, dataLength);

            return [.. rawData];
        }

        private static TcpClient CreateClient(string ipAddr, int tcpPort, int tcpTimeout)
        {
            // Move this to the front end!
            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            // string cleanedIP = Regex.Replace(ipAddr, @"\b0+(\d+)", "$1");

            TcpClient client = new TcpClient(ipAddr, tcpPort) // used to be cleanedIP
            {
                ReceiveTimeout = tcpTimeout,
                SendTimeout = tcpTimeout,
            };

            return client;
        }

        private static IModbusMaster CreateMaster(TcpClient client, int tcpTimeout)
        {
            ModbusFactory factory = new ModbusFactory();
            IModbusMaster master = factory.CreateMaster(client);

            master.Transport.ReadTimeout = tcpTimeout;
            master.Transport.WriteTimeout = tcpTimeout;
            master.Transport.Retries = 0;

            return master;
        }
    }
}
