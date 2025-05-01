using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FructoseLib.Extensions;
using FructoseLib.Network.Http.Settings;

using Newtonsoft.Json;

namespace FructoseLib.Network.Http
{
    /// <summary>
    /// HttpClientController manages getting least used and free according to Rate Limit HttpClient from HttpClientContainer collection
    /// </summary>

    public class Response
    {
        public string Body { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Error { get; set; }

        public Response()
        {
            Headers = new Dictionary<string, string>();
        }
    }

    public class RequestOptions
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public string Ja3 { get; set; }
        public string UserAgent { get; set; }
        public string Proxy { get; set; }
        public int Timeout { get; set; }

        public RequestOptions()
        {
            Headers = new Dictionary<string, string>();
        }
    }


    public class SecureHttpClient : IDisposable
    {
        [DllImport("/Libs/api-cycle-tls-client-v.1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MakeRequest(byte[] RequestOptions);

        [DllImport("/Libs/api-cycle-tls-client-v.1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Close();

        private string Proxy { get; init; } = string.Empty;
        private int Timeout { get; set; }
        public SecureHttpClient(WebProxy? WebProxy = null, int Timeout = 30)
        {
            this.Timeout = Timeout;

            if (WebProxy != null)
            {
                if (WebProxy.GetUsername() == null || WebProxy.GetPassword() == null)
                {
                    this.Proxy = $"{WebProxy.Address.Scheme}://{WebProxy.Address.Host}:{WebProxy.Address.Port}";
                }
                else
                {
                    this.Proxy = $"{WebProxy.Address.Scheme}://{WebProxy.GetUsername()}:{WebProxy.GetPassword()}@{WebProxy.Address.Host}:{WebProxy.Address.Port}";
                }
            }
        }

        public Response Get(string Url, Dictionary<string, string>? Headers)
        {
            return this.MakeRequest("GET", Url, Headers);
        }
        public Response Post(string Url, Dictionary<string, string>? Headers, string? Body)
        {
            return this.MakeRequest("POST", Url, Headers, Body);
        }
        public Response Patch(string Url, Dictionary<string, string>? Headers, string? Body)
        {
            return this.MakeRequest("PATCH", Url, Headers, Body);
        }
        public Response Put(string Url, Dictionary<string, string>? Headers, string? Body)
        {
            return this.MakeRequest("PUT", Url, Headers, Body);
        }
        public Response Delete(string Url, Dictionary<string, string>? Headers = null)
        {
            return this.MakeRequest("DELETE", Url, Headers);
        }
        private Response MakeRequest(string Method, string Url, Dictionary<string, string>? Headers, string? Body = null)
        {
            var RequestOptions = new RequestOptions
            {
                Method = Method,
                Url = Url,
                Body = Body ?? string.Empty,
                Headers = Headers ?? new(),
                Proxy = Proxy,
                Timeout = Timeout,
                Ja3 = Ja3Fingerprints.Random()
            };

            string RequestJson = JsonConvert.SerializeObject(RequestOptions);
            byte[] RequestBytes = Encoding.UTF8.GetBytes(RequestJson);

            IntPtr ResponsePointer = MakeRequest(RequestBytes);
            string ResponseJson = Marshal.PtrToStringAnsi(ResponsePointer);

            Response Responce = JsonConvert.DeserializeObject<Response>(ResponseJson);

            return Responce;
        }

        public static List<string> Ja3Fingerprints = new List<string>()
        {
            "771,4865-4866-4867-49196-49195-52393-49200-49199-52392-49162-49161-49172-49171-157-156-53-47-49160-49170-10,0-23-65281-10-11-16-5-13-18-51-45-43-27-21,29-23-24-25,0",
            "771,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-23-65281-10-11-35-16-5-13-18-51-45-43-27-21,29-23-24,0",
            "771,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-23-65281-10-11-35-16-5-13-18-51-45-43-27-17513-21,29-23-24,0",
            "771,4865-4867-4866-49195-49199-52393-52392-49196-49200-49162-49161-49171-49172-156-157-47-53,0-23-65281-10-11-35-16-5-34-51-43-13-45-28-21,29-23-24-25-256-257,0"
        };

        public void Dispose()
        {
            Close();
        }
    }

    public class SecureHttpClientController : IDisposable
    {
        private SemaphoreSlim Semaphore { get; set; }
        private List<SecureHttpClientContainer> SecureHttpClientContainers { get; init; }

        private long Throttling { get; init; }

        protected SecureHttpClientController(IEnumerable<WebProxy> WebProxy, HttpClientControllerSettings Settings)
        {
            this.SecureHttpClientContainers = new();
            this.Semaphore = new(0, Settings.MaxConcurrentPeeks < 1 ? 1 : Settings.MaxConcurrentPeeks);
            this.Semaphore.Release();
            this.Throttling = (long)(Settings.InTime.TotalMilliseconds / Settings.Rate);


            if (Settings.UseProxy == true)
            {

                foreach (var Item in WebProxy)
                {
                    SecureHttpClientContainers.Add(new(
                    Item,
                    new()
                    {
                        Timeout = Settings.Timeout,
                        UseProxy = true,
                        Throttling = Throttling
                    }));
                }
            }
            else
            {
                SecureHttpClientContainers.Add(new(
                    new()
                    {
                        Timeout = Settings.Timeout,
                        UseProxy = true,
                        Throttling = Throttling
                    }));
            }

        }

        public async Task<SecureHttpClient> GetFreeHttpClient()
        {
            Semaphore.Wait();
            try
            {
                while (!SecureHttpClientContainers.Where(Container => Container.Free).Any())
                {
                    await Task.Delay((int)Throttling / 2);
                }

                return SecureHttpClientContainers.Where(Container => Container.Free)
                    .OrderBy(Container => Container.Used)
                    .First().Client;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public void Dispose()
        {
            foreach(var Container in SecureHttpClientContainers)
            {
                Container.Dispose();
            }
        }
    }
}
