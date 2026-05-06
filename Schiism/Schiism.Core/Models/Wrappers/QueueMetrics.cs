using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Models.Wrappers
{
    public record QueueMetrics(
    int CurrentDepth,
    long TotalEnqueued,
    long TotalProcessed,
    long Dropped,
    DateTime LastEnqueueTime,
    DateTime LastDequeueTime);
}
