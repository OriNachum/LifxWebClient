using Infrared.Impl;

namespace LifxCoreController.Lightbulb
{
    public class BulbLogger : LifxBaseLogger
    {
        private string _fileName;

        protected override string FileName => _fileName;
        public BulbLogger(string bulbName)
        {
            _fileName = $"{ bulbName }.log";
        }
    }
}
