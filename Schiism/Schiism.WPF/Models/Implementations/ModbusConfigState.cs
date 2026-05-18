using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Abstractions.Modbus;

namespace Schiism.WPF.Models.Implementations
{
    public class ModbusConfigState : BindableBase, ICommandState
    {
        private IModbusConfig config;

        public IModbusConfig Config
        {
            get => config;
            private set => SetProperty(ref config, value);
        }

        public void Update(IModbusConfig conf)
        {
            config = conf;
        }
    }
}
