namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Enums;

    public interface IModbusInterpreter
    {
        // Interpret the received register data, according to parameters received
        List<string> InterpretRegs(List<ushort> rawData, ushort length, bool asciiEnable, DataSize selDataSize, NumericBase selNumericBase, Endian selEndian);
    }
}
