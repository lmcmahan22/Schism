namespace Schiism.WPF.Models
{
    public class ModbusWriteState : BindableBase
    {
        private ushort address;
        private string value;

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

        public void SetWrite(ushort address, string value)
        {
            this.Address = address;
            this.Value = value;
        }

        public void TriggerSend()
        {
            WriteSendTrigger?.Invoke(this, EventArgs.Empty);
        }
    }
}
