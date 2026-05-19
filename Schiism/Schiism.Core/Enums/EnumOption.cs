using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Enums
{
    public class EnumOption<T>
    {
        public required T Value { get; init; }

        public required string Display { get; init; }
    }
}
