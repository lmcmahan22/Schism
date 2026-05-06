using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC.Commands
{
    // Implemented by WPF
    // Implemented by Service (In Named Pipes, the sender hosts the client)
    public interface ICommandClient<TCommand>
    {
        Task SendAsync(TCommand command, CancellationToken ct);
    }
}
