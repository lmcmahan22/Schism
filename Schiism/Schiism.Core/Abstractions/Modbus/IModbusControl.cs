namespace Schiism.Core.Abstractions.Modbus
{
    public interface IModbusControl
    {
        bool RestartRequested { get; }

        void RequestRestart();

        void ClearRestartRequest();
    }
}
