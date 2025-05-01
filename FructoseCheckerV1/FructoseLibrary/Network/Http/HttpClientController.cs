using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FructoseLib.Network.Http.Settings;

namespace FructoseLib.Network.Http
{
    /// <summary>
    /// HttpClientController manages getting least used and free according to Rate Limit HttpClient from HttpClientContainer collection
    /// </summary>
    public class HttpClientController
    {
        private SemaphoreSlim Semaphore { get; set; }
        private List<HttpClientContainer> HttpClientContainers { get; init; }

        private long Throttling { get; init; }

        protected HttpClientController(IEnumerable<WebProxy> WebProxy, HttpClientControllerSettings Settings)
        {
            this.HttpClientContainers = new();
            this.Semaphore = new(0, Settings.MaxConcurrentPeeks < 1 ? 1 : Settings.MaxConcurrentPeeks);
            this.Semaphore.Release();
            this.Throttling = (long)(Settings.InTime.TotalMilliseconds / Settings.Rate);


            if (Settings.UseProxy == true)
            {

                foreach (var Item in WebProxy)
                {
                    HttpClientContainers.Add(new(
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
                HttpClientContainers.Add(new(
                    new()
                    {
                        Timeout = Settings.Timeout,
                        UseProxy = true,
                        Throttling = Throttling
                    }));
            }

        }

        public async Task<HttpClient> GetFreeHttpClient()
        {
            Semaphore.Wait();
            try
            {
                while (!HttpClientContainers.Where(Container => Container.Free).Any())
                {
                    await Task.Delay((int)Throttling / 2);
                }

                return HttpClientContainers.Where(Container => Container.Free)
                    .OrderBy(Container => Container.Used)
                    .First().Client;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public void PrintState()
        {
            Console.WriteLine("\n###########################################################");
            foreach (var Container in HttpClientContainers)
            {
                Console.WriteLine($" Id - {Container.Id} | Used - {Container.Used}");
            }
            Console.WriteLine("###########################################################\n");
        }
    }
}
