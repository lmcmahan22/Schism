// <copyright file="ModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Modbus
{
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NModbus;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.DTOs;

    /// <summary>
    /// Implementation class for the IModbusClient interface.
    /// </summary>
    public class ModbusClient
    {
        private readonly SemaphoreSlim connectionLock = new(1, 1);
        private readonly SemaphoreSlim modbusLock = new(1, 1);
        private readonly ILogger<ModbusClient> logger;
        private TcpClient? client;
        private IModbusMaster? master;

        public ModbusClient(ILogger<ModbusClient> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(ConfigState config)
        {
            await connectionLock.WaitAsync();
            try
            {
                if (client?.Connected == true)
                {
                    return;
                }

                client?.Dispose();

                client = CreateClient(config.IPAddress, config.TCPPort, config.TCPTimeout);

                master = CreateMaster(client, config.TCPTimeout);
            }
            catch
            {
                client?.Dispose();
                client = null;
                master = null;
                throw;
            }
            finally
            {
                connectionLock.Release();
            }
        }

        /// <inheritdoc/>
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
        //public async Task<List<ushort>> ReadData(ConfigState config)
        //{
        //    await modbusLock.WaitAsync();

        //    try
        //    {
        //        if (master == null)
        //        {
        //            throw new InvalidOperationException("Modbus client not connected.");
        //        }

        //        return config.SelectedPollType switch
        //        {
        //            PollType.InputStatus =>
        //                ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, true),

        //            PollType.HoldingRegisters =>
        //                ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, false),

        //            PollType.InputRegisters =>
        //                ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, true),

        //            _ =>
        //                ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, false),
        //        };
        //    }
        //    finally
        //    {
        //        modbusLock.Release();
        //    }
        //}

        public async Task<List<ushort>> ReadCoilDataAsync(ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected for Coil poll.");
                }

                return ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, false);
            }
            finally
            {
                modbusLock.Release();
            }
        }

        public async Task<List<ushort>> ReadRegisterDataAsync(ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected for Register poll.");
                }

                return ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, false);
            }
            finally
            {
                modbusLock.Release();
            }
        }

        public async Task WriteValueAsync(ModbusWriteDTO write, ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected.");
                }

                switch (write.Type)
                {
                    case PollType.CoilStatus:
                        await master.WriteSingleCoilAsync(
                            config.DeviceId,
                            write.Address,
                            write.Value != "0");
                        logger.LogInformation("Engine writing coil!");
                        break;

                    case PollType.HoldingRegisters:
                        await master.WriteSingleRegisterAsync(
                            config.DeviceId,
                            write.Address,
                            ushort.Parse(write.Value));
                        logger.LogInformation("Engine writing register!");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                modbusLock.Release();
            }
        }

        private List<ushort> ReadDigitals(
            IModbusMaster master,
            byte deviceId,
            ushort startAddress,
            ushort dataLength,
            bool isInputs)
        {
            List<bool> rawData = new List<bool>();

            // Reads ModbusData in chunks of 2000 coils, since that's the maximum allowed by the protocol. This prevents issues with trying to read too much data at once.
            for (int i = 0; i < dataLength; i += 2000)
            {
                ushort chunkSize = (ushort)Math.Min(2000, dataLength - i);
                bool[] chunkData = isInputs ? master.ReadInputs(deviceId, (ushort)(startAddress + i), chunkSize) : master.ReadCoils(deviceId, (ushort)(startAddress + i), chunkSize);
                rawData.AddRange(chunkData);
            }

            // Convert to ushorts, so bools can be displayed as 1s and 0s.
            // This also makes it so we can handle this data in a similar manner as register data, which returns as ushorts natively.
            // logger.LogInformation("Raw digital data read from Modbus device: {Data}", string.Join(", ", rawData.Select(x => x ? "1" : "0")));
            return [.. rawData.Select(x => Convert.ToUInt16(x))];
        }

        private List<ushort> ReadRegisters(
            IModbusMaster master,
            byte deviceId,
            ushort startAddress,
            ushort dataLength,
            bool isInputs)
        {
            List<ushort> rawData = new List<ushort>();

            // Reads ModbusData in chunks of 125 registers, since that's the maximum allowed by the protocol. This prevents issues with trying to read too much data at once.
            for (int i = 0; i < dataLength; i += 125)
            {
                ushort chunkSize = (ushort)Math.Min(125, dataLength - i);
                ushort[] chunkData = isInputs ? master.ReadInputRegisters(deviceId, (ushort)(startAddress + i), chunkSize) : master.ReadHoldingRegisters(deviceId, (ushort)(startAddress + i), chunkSize);
                rawData.AddRange(chunkData);
            }

            // logger.LogInformation("Raw register data read from Modbus device: {Data}", string.Join(", ", rawData));
            return rawData;
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
