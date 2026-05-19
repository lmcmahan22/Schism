namespace Schiism.WPF.Models.Implementations.States
{
    using Schiism.Core.Abstractions.IPC.States;

    public class StreamDataState<T> : BindableBase, IStreamDataState<T>
    {
        private T contents;

        public T Contents
        {
            get => contents;
            private set => SetProperty(ref contents, value);
        }

        public void Update (T cont)
        {
            Contents = cont;
        }
    }
}