using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC.Streams
{
    // Implemented by Service
    public interface IStreamPublisher<T>
    {
        Task StartAsync(CancellationToken ct);

        Task PublishAsync(T data, CancellationToken ct);

        Task AcceptLoopAsync(CancellationToken ct);
    }
}
