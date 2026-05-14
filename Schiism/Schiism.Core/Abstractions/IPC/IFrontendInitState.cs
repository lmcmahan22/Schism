using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC
{
    public interface IFrontendInitState
    {
        event Action<bool>? InitChanged;

        bool IsInitialized { get; }

        void SetInitialized(bool init);
    }
}
