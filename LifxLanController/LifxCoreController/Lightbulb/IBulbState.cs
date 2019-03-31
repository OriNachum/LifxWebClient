using System.Threading.Tasks;
using Lifx;

namespace LifxCoreController.Lightbulb
{
    public interface IBulbState
    {
        double? Brightness { get; }
        Power? Power { get; }
        int? Hue { get; }
        double? Saturation { get; }
        int? Temperature { get; }

        IBulbState Copy(bool? enforceLimits = null);
    }
}