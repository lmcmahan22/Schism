namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Handlers;
    using System.Threading.Tasks;

    public interface IEngineService
    {
        Task StartAsync(CancellationToken ct);

        Task StopAsync();

        void Configure(ModbusConfig config);
    }
}
