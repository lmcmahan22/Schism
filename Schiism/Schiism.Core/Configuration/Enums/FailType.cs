using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Configuration.Enums
{
    public enum FailType
    {
        [Description("Unknown")]
        Unknown,

        [Description("Good")]
        Good,

        [Description("Failed")]
        Failed,
    }
}
