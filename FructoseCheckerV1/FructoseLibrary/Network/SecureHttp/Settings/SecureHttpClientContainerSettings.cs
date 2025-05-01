using System;
using FructoseLib.Network.Http;

namespace FructoseLib.Network.Http.Settings
{
    public sealed class SecureHttpClientContainerSettings
    {
        public bool UseProxy = true;
        public TimeSpan Timeout = TimeSpan.FromMilliseconds(30000);
        public long Throttling = 0;
    }
}
