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
        Task PublishAsync(T data, CancellationToken ct);
    }
}
