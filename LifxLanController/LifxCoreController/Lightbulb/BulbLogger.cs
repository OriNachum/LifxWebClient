using Infrared.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace LifxCoreController.Lightbulb
{
    public class BulbLogger : LifxBaseLogger
    {
        private string _filePath;

        protected override string FilePath => _filePath;
        public BulbLogger(string filePath)
        {
            _filePath = filePath;
        }
    }
}
