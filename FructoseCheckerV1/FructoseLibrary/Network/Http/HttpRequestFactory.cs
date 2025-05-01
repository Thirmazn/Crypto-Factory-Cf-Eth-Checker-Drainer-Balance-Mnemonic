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
    public sealed class HttpRequestFactory : HttpClientController
    {
        private Func<HttpRequestException, HttpRequestException> HttpRequestExceptionHandler { get; init; }
        public HttpRequestFactory(IEnumerable<WebProxy> WebProxy, HttpRequestFactorySettings Settings, Func<HttpRequestException, HttpRequestException>? HttpRequestExceptionHandler = null)
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

        public async Task<string> GetAsync(string Url, CancellationToken Token, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url);
                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);
                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<string> PostAsync(string Url, string Data, CancellationToken Token, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Post, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<string> PutAsync(string Url, string Data, CancellationToken Token, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<string> GetAsync(string Url, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);
                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
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
                HttpRequestMessage Request = new(HttpMethod.Post, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType),
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<string> PutAsync(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<(string Responce, HttpResponseHeaders Headers)> GetAsyncWithResponseHeaders(string Url, CancellationToken Token, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url);

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpResponseHeaders Headers)> PostAsyncWithResponseHeaders(string Url, string Data, CancellationToken Token, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Post, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpResponseHeaders Headers)> PutAsyncWithResponseHeaders(string Url, string Data, CancellationToken Token, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<(string Responce, HttpResponseHeaders Headers)> GetAsyncWithResponseHeaders(string Url, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url);

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpResponseHeaders Headers)> PostAsyncWithResponseHeaders(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Post, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpResponseHeaders Headers)> PutAsyncWithResponseHeaders(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                try
                {
                    Response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException Ex)
                {
                    HttpRequestExceptionHandler(Ex);
                }

                return (await Response.Content.ReadAsStringAsync(), Response.Headers);
            }
            catch (Exception)
            {
                throw;
            }
        }

       
        public async Task<(string Responce, HttpStatusCode StatusCode)> GetAsyncWithStatusCode(string Url, CancellationToken Token, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url);

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                return (await Response.Content.ReadAsStringAsync(), Response.StatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<(string Responce, HttpStatusCode StatusCode)> PutAsyncWithStatusCode(string Url, string Data, CancellationToken Token, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request, Token);

                return (await Response.Content.ReadAsStringAsync(), Response.StatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<(string Responce, HttpStatusCode StatusCode)> GetAsyncWithStatusCode(string Url, Dictionary<string, string>? Headers = null)
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Get, Url);

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                return (await Response.Content.ReadAsStringAsync(), Response.StatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpStatusCode StatusCode)> PostAsyncWithStatusCode(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Post, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                return (await Response.Content.ReadAsStringAsync(), Response.StatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<(string Responce, HttpStatusCode StatusCode)> PutAsyncWithStatusCode(string Url, string Data, Dictionary<string, string>? Headers = null, string ContentType = "application/json")
        {
            try
            {
                HttpRequestMessage Request = new(HttpMethod.Put, Url)
                {
                    Content = new StringContent(Data, Encoding.UTF8, ContentType)
                };

                if (Headers != null)
                {
                    if (Headers.Any())
                    {
                        foreach (var Header in Headers)
                        {
                            Request.Headers.Add(Header.Key, Header.Value);
                        }
                    }
                }

                HttpResponseMessage Response = await (await GetFreeHttpClient()).SendAsync(Request);

                return (await Response.Content.ReadAsStringAsync(), Response.StatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
