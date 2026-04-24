// <copyright file="NModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Clients
{
    using System.Linq;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Enums;

    // TCP + MODBUS communication logic (interpretation of raw data is in the interpreter class, not here)
    public class NModbusClient : IModbusClient
    {
        public List<ushort> ReadData(string ip, int port, byte deviceId, ushort start, ushort length, int timeout, PollType dataType)
        {
            List<ushort> numericData = new List<ushort>();

            switch (dataType)
            {
                case PollType.InputStatus:
                    numericData = this.ReadIs(ip, port, deviceId, start, length, timeout);
                    break;
                case PollType.HoldingRegisters:
                    numericData = this.ReadHoldRs(ip, port, deviceId, start, length, timeout);
                    break;
                case PollType.InputRegisters:
                    numericData = this.ReadInputRs(ip, port, deviceId, start, length, timeout);
                    break;
                default:
                    // "ModbusDataType.CoilStatus"
                    numericData = this.ReadCs(ip, port, deviceId, start, length, timeout);
                    break;
            }

            return numericData;
        }

        public List<ushort> ReadCs(
            string ip,
            int port,
            byte deviceId,
            ushort start,
            ushort length,
            int timeout)
        {
            using TcpClient client = CreateClient(ip, port, timeout);
            IModbusMaster master = CreateMaster(client, timeout);

            bool[] rawData = master.ReadCoils(deviceId, start, length);
            return [.. rawData.Select(x => Convert.ToUInt16(x))];
        }

        public List<ushort> ReadIs(
            string ip,
            int port,
            byte deviceId,
            ushort start,
            ushort length,
            int timeout)
        {
            using TcpClient client = CreateClient(ip, port, timeout);
            IModbusMaster master = CreateMaster(client, timeout);

            bool[] rawData = master.ReadInputs(deviceId, start, length);
            return [.. rawData.Select(x => Convert.ToUInt16(x))];
        }

        public List<ushort> ReadHoldRs(
            string ip,
            int port,
            byte deviceId,
            ushort start,
            ushort length,
            int timeout)
        {
            using TcpClient client = CreateClient(ip, port, timeout);
            IModbusMaster master = CreateMaster(client, timeout);

            ushort[] rawData = master.ReadHoldingRegisters(deviceId, start, length);
            return [.. rawData];
        }

        public List<ushort> ReadInputRs(
            string ip,
            int port,
            byte deviceId,
            ushort start,
            ushort length,
            int timeout)
        {
            using TcpClient client = CreateClient(ip, port, timeout);
            IModbusMaster master = CreateMaster(client, timeout);

            ushort[] rawData = master.ReadInputRegisters(deviceId, start, length);
            return [.. rawData];
        }

        private static TcpClient CreateClient(string ip, int port, int timeout)
        {
            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            string cleanedIp = Regex.Replace(ip, @"\b0+(\d+)", "$1");

            TcpClient client = new TcpClient(cleanedIp, port)
            {
                ReceiveTimeout = timeout,
                SendTimeout = timeout,
            };

            return client;
        }

        private static IModbusMaster CreateMaster(TcpClient client, int timeout)
        {
            ModbusFactory factory = new ModbusFactory();
            IModbusMaster master = factory.CreateMaster(client);

            master.Transport.ReadTimeout = timeout;
            master.Transport.WriteTimeout = timeout;
            master.Transport.Retries = 0;

            return master;
        }
    }
}
