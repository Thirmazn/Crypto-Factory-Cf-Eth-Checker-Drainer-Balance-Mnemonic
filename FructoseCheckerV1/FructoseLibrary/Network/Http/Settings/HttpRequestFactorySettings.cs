using System;

namespace FructoseLib.Network.Http.Settings
{
    public class HttpRequestFactorySettings
    {
        public bool UseProxy = true;
        public TimeSpan Timeout = TimeSpan.FromMilliseconds(30000);
        public int MaxConcurrentPeeks = 1;
        public long Rate = 60;
        public TimeSpan InTime = TimeSpan.FromSeconds(60);
    }
}
