using System.ComponentModel;

namespace Schiism.WPF.Models
{
    public class EnumOption<T> : INotifyPropertyChanged
    {
        public T Value { get; set; }

        public string Display { get; set; }

        private bool isEnabled = true;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
