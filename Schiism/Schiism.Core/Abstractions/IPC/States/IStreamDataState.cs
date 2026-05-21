using Schiism.Core.Enums;
using Schiism.Core.Models.IPC.DTOs.Commands;
using Schiism.Core.Models.IPC.DTOs.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Abstractions.IPC.States
{
    public interface IStreamDataState<T>
    {
        T Contents { get; }

        void Update(T contents);
    }
}
