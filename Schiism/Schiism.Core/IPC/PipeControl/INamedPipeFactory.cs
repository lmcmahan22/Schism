using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.IPC.PipeControl
{
    public interface INamedPipeFactory
    {

        // Note that NamedPipe interactions only occur on objects with "Stream" in the name. Commands are streams here, just single sending streams that close immediately after.
        NamedPipeServerStream CreateNPServer(string pipeName);

        NamedPipeClientStream CreateNPClient(string pipeName);
    }
}
