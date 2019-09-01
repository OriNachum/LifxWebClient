using Infrared.Impl;

namespace ActionService
{
    public class ActionServiceLogger : LifxBaseLogger
    {
        protected override string FilePath
        {
            get
            {
                return $"C:\\Logs\\LifxWebApi\\ActionController.log";
            }
        }
    }
}
