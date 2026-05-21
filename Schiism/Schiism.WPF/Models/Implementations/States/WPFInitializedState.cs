namespace Schiism.WPF.Models.Implementations.States
{
    using Schiism.Core.Abstractions.IPC.States;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <inheritdoc/>
    public class WPFInitializedState : INotifyPropertyChanged, IInitializedState
    {
        private bool isInitialized;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get => this.isInitialized;
            set
            {
                this.isInitialized = value;
                this.OnPropertyChanged(nameof(this.IsInitialized));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
