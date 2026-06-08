using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.IPC.PipeControl
{
    public class AdminPipeFactory : INamedPipeFactory
    {

        public NamedPipeClientStream CreateNPClient(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public NamedPipeServerStream CreateNPServer(string pipeName)
        {
            var security = new PipeSecurity();

#pragma warning disable CA1416 // Validate platform compatibility
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            return NamedPipeServerStreamAcl.Create(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                security);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
