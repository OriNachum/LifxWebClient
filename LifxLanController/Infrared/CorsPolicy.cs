using Microsoft.AspNetCore.Cors.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared
{
    public class CorsPolicy
    {
        public static string Name => "SiteCorsPolicy";

        private static string[] _allowedSources;
        public static string[] AllowedSources {
            get
            {
                if (_allowedSources == null)
                {
                    _allowedSources = new string[]
                    {
                        "http://10.0.0.7",
                        "http://192.168.1.16",
                    };
                }

                return _allowedSources;
            }
        }
    }
}
