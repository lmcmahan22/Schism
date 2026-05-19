namespace Schiism.WPF.Models.Implementations.States
{
    using Schiism.Core.Abstractions.IPC.States;

    /// <inheritdoc/>
    public class InitializedState : IInitializedState
    {
        private volatile bool isInitialized;

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get => this.isInitialized;
            set => this.isInitialized = value;
        }
    }
}
