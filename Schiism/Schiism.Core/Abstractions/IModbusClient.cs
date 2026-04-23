namespace Schiism.Core.Abstractions {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NModbus;

    public interface IModbusClient
    {
        ushort[] ReadHoldingRegisters(byte deviceId, ushort start, ushort length);

        ushort[] ReadInputRegisters(byte deviceId, ushort start, ushort length);

        bool[] ReadCoils(byte deviceId, ushort start, ushort length);

        bool[] ReadInputs(byte deviceId, ushort start, ushort length);
    }
}
