using System;
using System.Net;
using System.Net.Http;

using FructoseLib.Extensions;
using FructoseLib.Network.Http.Settings;

namespace FructoseLib.Network.Http
{
    /// <summary>
    /// HttpClientContainer stores the HttpClient, connection information, number of uses, and the next time the client can be used.
    /// </summary>
    
    public sealed class SecureHttpClientContainer : IDisposable
    {
        private static int InitialId = 0;
        public SecureHttpClientContainer(WebProxy WebProxy, SecureHttpClientContainerSettings Settings)
        {
            InnerClient = new SecureHttpClient(WebProxy, (int)Settings.Timeout.TotalSeconds);

            this.Throttling = Settings.Throttling;
            this.UnlockTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.Id = InitialId++;
        }

        public SecureHttpClientContainer(SecureHttpClientContainerSettings Settings)
        {
            InnerClient = new SecureHttpClient(null, 30);

            Throttling = Settings.Throttling;
            UnlockTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.Id = InitialId++;
        }

        public int Id { get; set; }
        private SecureHttpClient InnerClient { get; init; }
        public SecureHttpClient Client
        {
            get
            {
                Used++;
                UnlockTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() + Throttling;
                return InnerClient;
            }
        }

        public long Used { get; private set; }
        public bool Free
        {
            get
            {
                return UnlockTimeStamp <= DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        private long UnlockTimeStamp { get; set; }
        private long Throttling { get; init; }

        public void Dispose()
        {
            this.InnerClient?.Dispose();
        }
    }
}
