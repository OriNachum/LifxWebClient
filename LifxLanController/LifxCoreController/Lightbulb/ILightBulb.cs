using Lifx;

namespace LifxCoreController.Lightbulb
{
    public interface ILightBulb : ILight
    {
        LightBulbState GetState();
    }
}