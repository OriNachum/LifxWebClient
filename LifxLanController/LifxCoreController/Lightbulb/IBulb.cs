using System.Threading;
using System.Threading.Tasks;
using Lifx;

namespace LifxCoreController.Lightbulb
{
    public interface IBulb
    {
        string Label { get; }
        IBulbState State { get; }

        string Serialize();

        Task<LightState> GetStateAsync(CancellationToken token);
        Task<(eLifxResponse response, string data)> OnOverTimeAsync(int v);
        Task<(eLifxResponse response, string message, string bulb)> SetStateOverTimeAsync(IBulbState state, long fadeInDurationValue);
        Task OnAsync();
        Task OffAsync();
        void Dispose();
        Task SetPowerAsync(Power power);
    }
}