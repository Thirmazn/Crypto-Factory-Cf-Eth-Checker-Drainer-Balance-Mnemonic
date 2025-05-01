using System;
using System.Net;
using System.Net.Http;

using FructoseLib.Network.Http.Settings;

namespace FructoseLib.Network.Http
{
    /// <summary>
    /// HttpClientContainer stores the HttpClient, connection information, number of uses, and the next time the client can be used.
    /// </summary>
    public sealed class HttpClientContainer
    {
        private static int InitialId = 0;
        public HttpClientContainer(WebProxy WebProxy, HttpClientContainerSettings Settings)
        {
            InnerClient = new HttpClient(
                new SocketsHttpHandler()
                {
                    Proxy = WebProxy,
                    UseProxy = true,
                    AutomaticDecompression = DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromSeconds(0),
                    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(0),
                    ResponseDrainTimeout = TimeSpan.FromSeconds(0),
                    Expect100ContinueTimeout = TimeSpan.FromSeconds(0),
                    MaxConnectionsPerServer = 256,
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    UseCookies = false,
                    PreAuthenticate = true,
                    SslOptions = new() 
                    {
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    },
                })
            {
                Timeout = Settings.Timeout,
            };


            this.Throttling = Settings.Throttling;
            this.UnlockTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.Id = InitialId++;
        }

        public HttpClientContainer(HttpClientContainerSettings Settings)
        {
            InnerClient = new HttpClient(
                new SocketsHttpHandler()
                {
                    UseProxy = false,
                    AutomaticDecompression = DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromSeconds(0),
                    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(0),
                    ResponseDrainTimeout = TimeSpan.FromSeconds(0),
                    Expect100ContinueTimeout = TimeSpan.FromSeconds(0),
                    MaxConnectionsPerServer = 256,
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    UseCookies = false
                })
            {
                Timeout = Settings.Timeout,
            };


            Throttling = Settings.Throttling;
            UnlockTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.Id = InitialId++;
        }
        public int Id { get; set; }
        private HttpClient InnerClient { get; init; }
        public HttpClient Client
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


    }
}
