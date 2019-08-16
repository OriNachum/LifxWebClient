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
        Task OnAsync();
        Task OnAsync(uint durationInMilliseconds);
        Task OffAsync();
        Task OffAsync(uint durationInMilliseconds);
        void Dispose();
        Task SetPowerAsync(Power power);
        Task SetPowerAsync(Power power, uint durationInMilliseconds);

        Task SetBrightnessAsync(Percentage brightness, uint durationInMilliseconds);
    }
}