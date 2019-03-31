using System.Threading.Tasks;

namespace LifxCoreController.Lightbulb
{
    public interface IAdvancedBulb : IBulb
    {
        Task<(eLifxResponse response, string message, string bulb)> SetStateOverTimeAsync(IBulbState state, long fadeInDurationValue);
    }
}