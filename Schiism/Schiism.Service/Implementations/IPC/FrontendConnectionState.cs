using Schiism.Core.Abstractions.IPC;

namespace Schiism.Service.Implementations.IPC
{
    public class FrontendInitState : IFrontendInitState
    {
        public event Action<bool>? InitChanged;

        private volatile bool isInitialized;

        public bool IsInitialized => this.isInitialized;

        public void SetInitialized(bool init)
        {
            if (this.isInitialized == init)
            {
                return;
            }

            this.isInitialized = init;
            this.InitChanged?.Invoke(init);
        }
    }
}
