using FructoseLib.Network.Http.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FructoseLib.Network.Http
{
    /// <summary>
    /// Smart HttpClient wrapper, able to work both with and without a proxy, limit bandwidth and load balance between HttpClient with a connected proxy evenly
    /// </summary>
   
    public sealed class SecureHttpRequestFactory : SecureHttpClientController, IDisposable
    {
        private Func<HttpRequestException, HttpRequestException> HttpRequestExceptionHandler { get; init; }
        public SecureHttpRequestFactory(IEnumerable<WebProxy> WebProxy, SecureHttpRequestFactorySettings Settings, Func<HttpRequestException, HttpRequestException>? HttpRequestExceptionHandler = null)
            : base(
                      WebProxy,
                      new HttpClientControllerSettings()
                      {
                          UseProxy = Settings.UseProxy,
                          Timeout = Settings.Timeout,
                          MaxConcurrentPeeks = Settings.MaxConcurrentPeeks,
                          Rate = Settings.Rate,
                          InTime = Settings.InTime
                      })
        {
            if (HttpRequestExceptionHandler == null)
            {
                this.HttpRequestExceptionHandler = delegate (HttpRequestException Ex) { throw Ex; };
            }
            else
            {
                this.HttpRequestExceptionHandler = HttpRequestExceptionHandler;
            }
        }

        public async Task<string> GetAsync(string Url, Dictionary<string, string>? Headers = null)
        {
            try
            {
                var Response = (await GetFreeHttpClient()).Get(Url, Headers);

                try
                {
                    if(Response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException($"Response status code does not indicate success: {(int)Response.StatusCode} ({Response.StatusCode.ToString()}) | {Response.Body}", null, Response.StatusCode);
                    }
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return Response.Body;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> PostAsync(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                Headers.Add("content-type", ContentType);

                var Response = (await GetFreeHttpClient()).Post(Url, Headers, Data);

                try
                {
                    if (Response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException($"Response status code does not indicate success: {(int)Response.StatusCode} ({Response.StatusCode.ToString()})");
                    }
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return Response.Body;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            base.Dispose();
        }
    }
}
