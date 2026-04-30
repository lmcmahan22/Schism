// <copyright file="ModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Handlers
{
    using System.Linq;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Enums;

    /// <inheritdoc/>
    public class ModbusClient : IModbusClient
    {
        /// <inheritdoc/>
        public List<ushort> ReadData(IModbusConfig config)
        {
            List<ushort> numericData = new List<ushort>();

            using TcpClient client = CreateClient(config.IPAddress, config.TCPPort, config.TCPTimeout);
            IModbusMaster master = CreateMaster(client, config.TCPTimeout);

            switch (config.SelectedPollType)
            {
                case PollType.InputStatus:
                    numericData = ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, true);
                    break;
                case PollType.HoldingRegisters:
                    numericData = ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, false);
                    break;
                case PollType.InputRegisters:
                    numericData = ReadRegisters(master, config.DeviceId, config.StartAddress, config.DataLength, true);
                    break;
                default:
                    // "PollType.CoilStatus"
                    numericData = ReadDigitals(master, config.DeviceId, config.StartAddress, config.DataLength, false);
                    break;
            }

            return numericData;
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
            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            string cleanedIP = Regex.Replace(ipAddr, @"\b0+(\d+)", "$1");

            TcpClient client = new TcpClient(cleanedIP, tcpPort)
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
