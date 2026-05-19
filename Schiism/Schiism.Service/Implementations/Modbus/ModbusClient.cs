// <copyright file="ModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.Modbus
{
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Enums;

    /// <summary>
    /// Implementation class for the IModbusClient interface.
    /// </summary>
    public class ModbusClient : IModbusClient
    {
        private readonly SemaphoreSlim connectionLock = new(1, 1);
        private TcpClient? client;
        private IModbusMaster? master;

        /// <inheritdoc/>
        public async Task InitializeAsync(IConfigState config)
        {
            await this.connectionLock.WaitAsync();
            try
            {
                if (this.client?.Connected == true)
                {
                    return;
                }

                this.client = CreateClient(config.IPAddress, config.TCPPort, config.TCPTimeout);

                // TcpClient constructor connects synchronously, so just wrap for consistency
                await Task.CompletedTask;

                this.master = CreateMaster(this.client, config.TCPTimeout);
            }
            finally
            {
                this.connectionLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            await this.connectionLock.WaitAsync();
            try
            {
                this.master?.Dispose();
                this.master = null;

                if (this.client != null)
                {
                    this.client.Close();
                    this.client.Dispose();
                    this.client = null;
                }
            }
            finally
            {
                this.connectionLock.Release();
            }
        }

        /// <inheritdoc/>
        public List<ushort> ReadData(IConfigState config)
        {
            if (this.master == null)
            {
                throw new InvalidOperationException("Modbus client not connected.");
            }

            return config.SelectedPollType switch
            {
                PollType.InputStatus =>
                    ReadDigitals(this.master, config.DeviceId, config.StartAddress, config.DataLength, true),

                PollType.HoldingRegisters =>
                    ReadRegisters(this.master, config.DeviceId, config.StartAddress, config.DataLength, false),

                PollType.InputRegisters =>
                    ReadRegisters(this.master, config.DeviceId, config.StartAddress, config.DataLength, true),

                _ =>
                    ReadDigitals(this.master, config.DeviceId, config.StartAddress, config.DataLength, false),
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
            IPAddress ip = IPAddress.Parse(ipAddr);

            TcpClient client = new TcpClient(AddressFamily.InterNetwork);

            client.ReceiveTimeout = tcpTimeout;
            client.SendTimeout = tcpTimeout;

            client.Connect(ip, tcpPort);

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
