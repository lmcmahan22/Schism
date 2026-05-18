using Schiism.Core.Abstractions.Modbus;
using System.ComponentModel;

namespace Schiism.Core.Abstractions.IPC.States
{
    public interface ICommandState : INotifyPropertyChanged
    {
        IModbusConfig Config { get; }

        void Update(IModbusConfig conf);
    }
}
