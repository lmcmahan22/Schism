using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC
{
    // Implemented by WPF
    public interface IStreamSubscriber<T>
    {
        Task StartAsync(Func<T, Task> onData, CancellationToken ct);
    }
}
