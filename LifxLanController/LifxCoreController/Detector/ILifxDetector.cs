using Lifx;
using LifxCoreController.Lightbulb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxCoreController.Detector
{
    public interface ILifxDetector : IDisposable
    {
        IDictionary<IPAddress, IAdvancedBulb> Bulbs { get;  }

        Task DetectLightsAsync(CancellationToken cancellationToken);
    }
}
