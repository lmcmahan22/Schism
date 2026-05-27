namespace Schiism.Core.Abstractions.RuntimeControl
{
    using Schiism.Core.Models.RuntimeControl;

    public interface IServiceSettingsStore
    {

        ServiceRuntimeSettings Load();

        void Save(ServiceRuntimeSettings settings);
    }
}
