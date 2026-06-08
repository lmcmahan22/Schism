using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.IPC.PipeControl
{
    public class BasePipeFactory : INamedPipeFactory
    {
        // public NamedPipeServerStream Create(string pipeName)
        //    => new NamedPipeServerStream(
        //        pipeName,
        //        PipeDirection.In,
        //        NamedPipeServerStream.MaxAllowedServerInstances,
        //        PipeTransmissionMode.Byte,
        //        PipeOptions.Asynchronous);

        public NamedPipeClientStream CreateNPClient(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public NamedPipeServerStream CreateNPServer(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
    }
}
