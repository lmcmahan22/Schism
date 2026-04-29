namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Handlers;
    using System.Threading.Tasks;

    // Works a lot like IHostedService. Should we be implementing that interface from within this interface???
    public interface IEngineService
    {
        void Configure(ModbusConfig config);

        Task RunAsync(CancellationToken token);
    }
}
