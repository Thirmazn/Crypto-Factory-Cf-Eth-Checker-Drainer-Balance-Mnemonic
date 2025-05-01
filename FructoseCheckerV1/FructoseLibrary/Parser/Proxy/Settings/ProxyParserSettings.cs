using System;
using System.Threading.Tasks;

namespace FructoseLib.Parser.Proxy.Settings
{
    public sealed record ProxyParserSettings
    {
        public int MaxUrlProxyLoadingTries { get; set; } = 3;
        public int ProxyCheckingThreads { get; set; } = 200;
        public bool OnlyHttpMode { get; set; } = false;
        public bool OnlyRotatingMode { get; set; } = false;
        public bool OnlyIpv6Mode { get; set; } = true; 
        public Func<string, Task> SetDefaultProxyLocationCallback { get; set; } = null;

        
    }
}
