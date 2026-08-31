// <copyright file="ModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Modbus
{
    using Microsoft.Extensions.Logging;
    using NModbus;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.DTOs;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

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

        public async Task<List<ushort>> ReadCoilDataAsync(ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected to server for Coil poll.");
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
                    throw new InvalidOperationException("Modbus client not connected to server for Register poll.");
                }

                return ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, false);
            }
            finally
            {
                modbusLock.Release();
            }
        }

        public async Task Heartbeat(CancellationToken stoppingToken, ConfigState config)
        {
            bool hbRaised;

            await modbusLock.WaitAsync();

            // Read Heartbeat status, then release lock
            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected to server.");
                }

                // Read the heartbeat value from a length 1 coil read
                List<ushort> hbRawResult = ReadDigitals(this.master, config.DeviceId, 2100, 1, false);
                hbRaised = hbRawResult[0] == 1;
            }
            finally
            {
                modbusLock.Release();
            }

            // If heartbeat was raised, stick around. Otherwise, leave.
            if (!hbRaised)
                return;

            // Don't do this with the lock! Remove delay if not actually needed!
            // NOTE: PMX uses 1000ms for its CheckAlive messages. We can go as low as 1000ms here to match that, but I'd prefer to stick with 3000ms on the PLC and PC.
            logger.LogInformation("[CORE] Heartbeat wait begin!");
            await Task.Delay(1000, stoppingToken);
            logger.LogInformation("[CORE] Heartbeat wait end!");

            await modbusLock.WaitAsync();

            // If PLC set this value to 1, set it back to 0, once more with the lock acquired.
            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException(
                        "Modbus client not connected to server.");
                }

                logger.LogInformation("[CORE] Heartbeat delay has passed. Dropping heartbeat coil!");
                await master.WriteSingleCoilAsync(
                        config.DeviceId,
                        2100,
                        false);
            }
            finally
            {
                modbusLock.Release();
            }
        }

        public async Task WriteValue(ModbusWriteDTO write, ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected to server.");
                }

                switch (write.Type)
                {
                    case PollType.CoilStatus:
                        logger.LogInformation("[CORE] Engine writing {0} on coil {1}", write.Value, write.Address);
                        await this.master.WriteSingleCoilAsync(
                            config.DeviceId,
                            write.Address,
                            write.Value != "0");
                        break;

                    case PollType.HoldingRegisters:
                        logger.LogInformation("[CORE] Engine writing {0} to register {1}", write.Value, write.Address);
                        await master.WriteSingleRegisterAsync(
                            config.DeviceId,
                            write.Address,
                            ushort.Parse(write.Value));
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

        // Liam do these in Big Endian Byte-Swapped!!!
        public async Task WriteBoardAvailableAsync(BoardAvailableDTO baDTO, ConfigState config)
        {
            await modbusLock.WaitAsync();

            try
            {
                if (master == null)
                {
                    throw new InvalidOperationException("Modbus client not connected to server.");
                }

                var hermesRegisters = new List<ushort>();

                // Add register data to the list in the correct order, so we can write it all at once.
                hermesRegisters.AddRange(this.StringToRegistersByteSwap(baDTO.BoardId, 18));
                hermesRegisters.Add(this.StringToWidth(baDTO.Width));
                hermesRegisters.Add((ushort)baDTO.FailedBoard);
                hermesRegisters.Add((ushort)baDTO.FlippedBoard);
                hermesRegisters.AddRange(this.StringToRegistersByteSwap(baDTO.TopBarcode, 10));
                hermesRegisters.AddRange(this.StringToRegistersByteSwap(baDTO.BottomBarcode, 10));

                var vendorRegisters = new List<ushort>();
                vendorRegisters.AddRange(this.StringToRegistersByteSwap(baDTO.PartName, 11));

                // Send board available SMEMA
                logger.LogInformation("[CORE] Engine disengaging UPBA coil!");
                await this.master.WriteSingleCoilAsync(
                    config.DeviceId,
                    2121,
                    false);

                logger.LogInformation("[CORE] Engine disengaging UPBA coil!");
                await this.master.WriteSingleCoilAsync(
                    config.DeviceId,
                    2123,
                    false);

                logger.LogInformation("[CORE] BoardAvailable Hermes register write attempt for BoardID: {0}", baDTO.BoardId);

                await this.master.WriteMultipleRegistersAsync(
                    config.DeviceId,
                    2101,
                    hermesRegisters.ToArray());

                logger.LogInformation("[CORE] BoardAvailable Vendor register write attempt for BoardID: {0}", baDTO.BoardId);

                await this.master.WriteMultipleRegistersAsync(
                    config.DeviceId,
                    2300,
                    vendorRegisters.ToArray());

                // Send board available SMEMA
                if (baDTO.ReceiptDir)
                {
                    logger.LogInformation("[CORE] Engine engaging DNBA coil!");
                    await this.master.WriteSingleCoilAsync(
                        config.DeviceId,
                        2123,
                        true);
                }
                else
                {
                    logger.LogInformation("[CORE] Engine engaging UPBA coil!");
                    await this.master.WriteSingleCoilAsync(
                        config.DeviceId,
                        2121,
                        true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to implement BoardAvailable contents for PartName {PartName}. Error message: {ex}",
                    baDTO.PartName, ex);

                throw;
            }
            finally
            {
                modbusLock.Release();
            }
        }

        private ushort[] StringToRegistersByteSwap(string value, int registerCount)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            var bytes = Encoding.ASCII.GetBytes(value);
            var registers = new ushort[registerCount];

            for (int i = 0; i < registerCount; i++)
            {
                int byteIndex = i * 2;

                byte high = byteIndex < bytes.Length
                    ? bytes[byteIndex]
                    : (byte)0;

                byte low = byteIndex + 1 < bytes.Length
                    ? bytes[byteIndex + 1]
                    : (byte)0;

                // Low and High are byte swapped! If you don't want this, swap their positions here
                registers[i] = (ushort)((low << 8) | high);

                logger.LogInformation("[CORE] Building Registers Array: {1}", string.Join(", ", registers));
            }

            return registers;
        }

        // We need to do it this way, because converting a numeric value on a single short vs. translating ASCII characters to bytes on a single short are NOT the same process!
        private ushort StringToWidth(string value)
        {
            if (value is null or "--" || value == string.Empty)
            {
                return 0x2D2D;
            }
            else
            {
                return Convert.ToUInt16(value);
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
            // logger.LogInformation("[CORE] Raw digital data read from Modbus device: {Data}", string.Join(", ", rawData.Select(x => x ? "1" : "0")));
            // logger.LogInformation("[CORE] Received MODBUS Coil Data from Server.");
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

            // logger.LogInformation("[CORE] Raw register data read from Modbus device: {Data}", string.Join(", ", rawData));
            // logger.LogInformation("[CORE] Received MODBUS Register Data from Server.");
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
