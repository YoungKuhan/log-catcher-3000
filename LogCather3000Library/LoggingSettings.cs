using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogCather3000Library
{
    public class LoggingSettings
    {
        public bool EnableRequestLogging { get; set; }
        public bool EnableResponseLogging { get; set; }
        public bool ExternalApiLogging { get; set; }
    }
}
