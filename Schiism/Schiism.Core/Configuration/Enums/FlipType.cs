using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Configuration.Enums
{
    public enum FlipType
    {
        [Description("Unknown")]
        Unknown,

        [Description("Not Flipped")]
        NotFlipped,

        [Description("Flipped")]
        Flipped,
    }
}
