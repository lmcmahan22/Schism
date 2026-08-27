using Schiism.Core.Configuration.Enums;

namespace Schiism.WPF.Models
{
    public class ModbusWriteState : BindableBase
    {
        private ushort address;
        private string value;

        private PollType selPollType;

        public event EventHandler? WriteSendTrigger;

        public ushort Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        public string Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        public PollType SelectedPollType
        {
            get => selPollType;
            set => SetProperty(ref this.selPollType, value);
        }

        public void SendWrite(PollType selPol, ushort address, string value)
        {
            this.selPollType = selPol;
            this.Address = address;
            this.Value = value;

            WriteSendTrigger?.Invoke(this, EventArgs.Empty);
        }
    }
}
