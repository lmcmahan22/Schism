using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC
{
    // Implemented by Service (In Named Pipes, the receiver hosts the server)
    public interface ICommandServer<TCommand>
    {
        Task StartAsync(Func<TCommand, Task> handler, CancellationToken ct);
    }
}
