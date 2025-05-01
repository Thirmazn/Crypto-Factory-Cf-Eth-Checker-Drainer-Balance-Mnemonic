using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FructoseCheckerV1.Utils;

using FructoseLib.Extensions;
using FructoseLib.Network.Http;

using Newtonsoft.Json;

namespace FructoseCheckerV1
{
    internal class Network
    {
        static Regex EventRegex = new(@"{.*}");
        public static async Task<string> GetEventAsync(string Url, string EventName, Dictionary<string, string> Headers = null, WebProxy Proxy = null, int Timeout = 10000)
        {
            var Client = new SecureHttpClient();

            try
            {
                var Response = Client.Get(Url, Headers);
                
                if(Response.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpRequestException($"Status code - {Response.StatusCode}");
                }

                foreach (var Line in Response.Body.Split("event: "))
                {
                    if (Line.StartsWith(EventName))
                    {
                        return EventRegex.Match(Line).Value;
                    }
                }

                return "{}";
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<string> GetAsync(string Url, Dictionary<string, string> Headers = null, WebProxy Proxy = null, int Timeout = 10000)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            HttpClientHandler Handler = new()
            {
                Proxy = Proxy,
                UseProxy = Proxy == null ? false : true,
                MaxConnectionsPerServer = 256
            };

            HttpClient Client = new(Handler)
            {
                Timeout = TimeSpan.FromMilliseconds(Timeout),
            };

            Client.DefaultRequestHeaders.Add("user-agent", new Random().UserAgent());

            if (Headers != null)
            {
                if (Headers.Where(Header => Header.Key.Contains("Bearer")).Any())
                {
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.ElementAt(0).Key, Headers.ElementAt(0).Value);
                    Headers.Remove(Headers.ElementAt(0).Key);
                }

                if (Headers.Any())
                {
                    foreach (var Header in Headers)
                    {
                        Client.DefaultRequestHeaders.Add(Header.Key, Header.Value);
                    }
                }
            }

            try
            {
                HttpResponseMessage Response = await Client.GetAsync(Url, token);
                Response.EnsureSuccessStatusCode();
                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Handler.Dispose();
                Client.Dispose();
                source.Dispose();
            }
        }
        public static async Task<string> GetAsyncDisableTLSCheck(string Url, Dictionary<string, string> Headers = null, WebProxy Proxy = null, int Timeout = 10000)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            HttpClient Client = new HttpClient(
                new SocketsHttpHandler()
                {
                    Proxy = Proxy,
                    UseProxy = Proxy == null ? false : true,
                    AutomaticDecompression = DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromSeconds(0),
                    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(0),
                    ResponseDrainTimeout = TimeSpan.FromSeconds(0),
                    Expect100ContinueTimeout = TimeSpan.FromSeconds(0),
                    MaxConnectionsPerServer = 10,
                    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (Sender, Certificate, Chain, SslPolicyErrors) => true,

                    },
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    UseCookies = false,
                })
            {
                Timeout = TimeSpan.FromSeconds(Timeout),
            };

            Client.DefaultRequestHeaders.Add("user-agent", new Random().UserAgent());

            if (Headers != null)
            {
                if (Headers.Where(Header => Header.Key.Contains("Bearer")).Any())
                {
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.ElementAt(0).Key, Headers.ElementAt(0).Value);
                    Headers.Remove(Headers.ElementAt(0).Key);
                }

                if (Headers.Any())
                {
                    foreach (var Header in Headers)
                    {
                        Client.DefaultRequestHeaders.Add(Header.Key, Header.Value);
                    }
                }
            }

            try
            {
                HttpResponseMessage Response = await Client.GetAsync(Url, token);
                Response.EnsureSuccessStatusCode();
                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Client.Dispose();
                source.Dispose();
            }
        }
        public static async Task<string> GetAsyncSecure(string Url, Dictionary<string, string> Headers = null, WebProxy Proxy = null, int Timeout = 10000)
        {
            SecureHttpClient SecureClient = new(Proxy, Timeout);

                try
                {
                    var Responce = SecureClient.Get(Url, Headers);

                    if (Responce.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException($"GET - {Url}", null, Responce.StatusCode);
                    }
                    return Responce.Body;
                }
                catch (Exception)
                {
                    throw;
                }

        }

        public static async Task<string> PostAsync(string Url, string Data, string ContentType = "application/json", Dictionary<string, string> Headers = null, WebProxy Proxy = null, int Timeout = 10000)
        {
            using HttpClientHandler Handler = new()
            {
                Proxy = Proxy,
                UseProxy = true,
                MaxConnectionsPerServer = 256,
            };

            using HttpClient Client = new(Handler)
            {
                Timeout = TimeSpan.FromMilliseconds(Timeout),
            };

            Client.DefaultRequestHeaders.UserAgent.ParseAdd(new Random().UserAgent());

            if (Headers != null)
            {
                if (Headers.Where(Header => Header.Key.Contains("Bearer")).Any())
                {
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Headers.ElementAt(0).Key, Headers.ElementAt(0).Value);
                    Headers.Remove(Headers.ElementAt(0).Key);
                }

                if (Headers.Any())
                {
                    foreach (var Header in Headers)
                    {
                        Client.DefaultRequestHeaders.Add(Header.Key, Header.Value);
                    }
                }

            }

            try
            {
                HttpResponseMessage Response = await Client.PostAsync(Url, new StringContent(Data, Encoding.UTF8, ContentType));
                Response.EnsureSuccessStatusCode();
                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
