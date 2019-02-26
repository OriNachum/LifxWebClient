using Lifx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxLanController
{
    public interface ILifxDetector : IDisposable
    {
        IDictionary<IPAddress, ILight> Lights { get;  }

        Task DetectLights(CancellationToken cancellationToken);
    }
}
