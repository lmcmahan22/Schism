using Schiism.Core.Abstractions.Modbus;

namespace Schiism.Service.Models.Implementations.Modbus
{
    public class ModbusControl : IModbusControl
    {
        public bool RestartRequested { get; private set; }

        public void RequestRestart()
        {
            RestartRequested = true;
        }

        public void ClearRestartRequest()
        {
            RestartRequested = false;
        }
    }
}
