using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions
{
    // Engine-based Logger object to replace .Service's Windows based Logger object. Originally required the app to use a Windows dependency/package, but that's no longer the case with this dependency.
    public interface IEngineLogger
    {
        void Info(string message);

        void Warning(string message);

        void Error(string message, Exception ex);
    }
}
