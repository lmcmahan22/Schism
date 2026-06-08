using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.IPC.Streams
{
    public class StreamStore<T> : INotifyPropertyChanged
    {
        private T contents;

        public T Contents
        {
            get => contents;
            private set
            {
                contents = value;

                // OnPropertyChanged takes the property name, not the field name! It's not OnFieldChanged!
                OnPropertyChanged(nameof(Contents));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Update(T cont)
        {
            Contents = cont;
        }
    }
}
