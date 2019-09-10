using Infrared.Impl;

namespace ActionService
{
    public class ActionServiceLogger : LifxBaseLogger
    {
        protected override string FileName
        {
            get
            {
                return $"ActionController.log";
            }
        }
    }
}
