using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using FructoseCheckerV1.Debank.Types;
using FructoseCheckerV1.Models;

using FructoseLib.Extensions;
using FructoseLib.Network.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Polly;

using Tokens = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Factory
{
    static class DebankWrapper
    {
        public static DebankClient DebankClient { get; private set; }
        public static void Init(IEnumerable<WebProxy> Proxy, bool HideProtocolBalances)
        {
            DebankClient = new(Proxy, HideProtocolBalances);
        }
    }

    public class DebankSigner
    {
        private string GetSha256(string Data)
        {
            using (SHA256 SHA256 = SHA256.Create())
            {
                byte[] Bytes = SHA256.ComputeHash(Encoding.UTF8.GetBytes(Data));

                StringBuilder Builder = new StringBuilder();
                for (int Index = 0; Index < Bytes.Length; Index++)
                {
                    Builder.Append(Bytes[Index].ToString("x2"));
                }

                return Builder.ToString();
            }
        }

        private string GetHmac(string Data, string Key)
        {
            using (HMACSHA256 HMACSHA256 = new HMACSHA256(Encoding.UTF8.GetBytes(Key)))
            {
                byte[] DataBytes = Encoding.UTF8.GetBytes(Data);
                byte[] Signature = HMACSHA256.ComputeHash(DataBytes);

                StringBuilder Builder = new StringBuilder();
                for (int Index = 0; Index < Signature.Length; Index++)
                {
                    Builder.Append(Signature[Index].ToString("x2"));
                }
                return Builder.ToString();
            }
        }

        private string GetNonce()
        {
            return $"n_{new Random().String(40)}";
        }

        private string GetSign(string Method, string Path, string Query, string Nonce, long TimeStamp)
        {
            string Key = GetSha256($"debank-api\n{Nonce}\n{TimeStamp}");
            string Data = GetSha256($"{Method}\n{Path}\n{Query}");
            return GetHmac(Data, Key);
        }

        public Dictionary<string, string> CreateHeaders(string Url)
        {
            var Uri = new Uri(Url);
            var QueryParsed = HttpUtility.ParseQueryString(Uri.Query);
            var Query = string.Join("&", QueryParsed.AllKeys.Select(Key => $"{Key}={QueryParsed[Key]}").Reverse());
            var Nonce = GetNonce();
            long Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var Signature = GetSign("GET", Uri.AbsolutePath, Query, Nonce, Timestamp);


            return new Dictionary<string, string>()
                {
                    { "account", $"{{\"random_at\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()},\"random_id\":\"{new Random().String(32)}\",\"user_addr\":null}}" },
                    { "origin" , "https://debank.com"},
                    { "referer" , "https://debank.com/"},
                    { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36" },
                    { "x-api-ver", "v2"},
                    { "x-api-ts",  Timestamp.ToString()},
                    { "x-api-sign",  Signature},
                    { "x-api-nonce",  Nonce},
                    
                };
        }

        public Dictionary<string, string> CreateHeaders(string Url, string Nonce, long Timestamp)
        {
            var Uri = new Uri(Url);
            var QueryParsed = HttpUtility.ParseQueryString(Uri.Query);
            var Query = string.Join("&", QueryParsed.AllKeys.Select(Key => $"{Key}={QueryParsed[Key]}").Reverse());
            var Signature = GetSign("GET", Uri.AbsolutePath, Query, Nonce, Timestamp);


            return new Dictionary<string, string>()
                {
                    { "x-api-ver", "v2"},
                    { "x-api-ts",  Timestamp.ToString()},
                    { "x-api-sign",  Signature},
                    { "x-api-nonce",  Nonce},
                };
        }
    }

    public class DebankClient
    {
        private DebankSigner DebankSigner { get; set; }
        private SecureHttpRequestFactory Request { get; set; }
        private bool HideProtocolBalances { get; set; }
        public DebankClient(bool HideProtocolBalances)
        {
            this.HideProtocolBalances = HideProtocolBalances;
            Request = new SecureHttpRequestFactory(
                        null,
                        new()
                        {
                            Rate = 2000,
                            InTime = TimeSpan.FromMilliseconds(60),
                            Timeout = TimeSpan.FromMilliseconds(30000),
                            UseProxy = false,
                            MaxConcurrentPeeks = 25
                        },
                        delegate (HttpRequestException Ex)
                        {
                            throw Ex;
                        }
                    );

            DebankSigner = new();
        }

        public DebankClient(IEnumerable<WebProxy> Proxy, bool HideProtocolBalances)
        {
            this.HideProtocolBalances = HideProtocolBalances;
            Request = new SecureHttpRequestFactory(
                Proxy,
                new()
                {
                    Rate = 9999,
                    InTime = TimeSpan.FromMilliseconds(60),
                    Timeout = TimeSpan.FromMilliseconds(30000),
                    UseProxy = true,
                    MaxConcurrentPeeks = 500
                },
                delegate (HttpRequestException Ex)
                {
                    throw Ex;
                }
            );

            DebankSigner = new();
        }

        private static string[] FakeProtocolsSignatures = new string[]
        {
            "VEROX",
            "ShibaSwap",
            "Alpaca Finance",
            "Olympus",
            "Votium",
            "Alpha Homora",
            "Venus",
            "ChargeDeFi",
            "Moonpot",
            "Belt Finance",
            "Shell V2",
            "Velodrome V2",
            "SyncSwap",
            "Aerodrome",
            "LIDO",
            "Pika Protocol",
            "Pika Protocol V4",
            "prePO",
            "Mean Finance",
            "Uniswap V3",
            "SushiSwap",
            "Hop Protocol",
            "Balancer V2",
            "Dao Maker",
            "Zunami Protocol",
            "Gnosis"
        };

        private static string[] FakeProtocolsStopSignatures = new string[]
        {
            "OpenEden",
        };



        public async Task<IEnumerable<(string Chain, IEnumerable<FructoseCheckerV1.Debank.Types.Token> Tokens)>> GetTokens(string Address)
        {
            var UsedChains = await GetUsedChains(Address);

            ConcurrentQueue<(string Chain, IEnumerable<FructoseCheckerV1.Debank.Types.Token> Tokens)> Tokens = new();

            if(UsedChains.Count() > 0)
            {
                await Parallel.ForEachAsync(UsedChains, new ParallelOptions() { MaxDegreeOfParallelism = UsedChains.Count() }, async (UsedChain, Token) =>
                {
                    string Url = $"https://api.debank.com/token/balance_list?user_addr={Address.ToLower()}&chain={UsedChain}";
                    var ResponceBody = GetResponceBody(await Request.GetAsync(Url, this.DebankSigner.CreateHeaders(Url)));
                    Tokens.Enqueue((UsedChain.ToUpper(), JsonConvert.DeserializeObject<IEnumerable<FructoseCheckerV1.Debank.Types.Token>>(ResponceBody)));
                });

                
            }
            return Tokens;
        }

        public async Task<IEnumerable<(string Chain, string Name, double Balance)>> GetProtocols(string Address, int Tries = 0)
        {
            if(HideProtocolBalances == true)
            {
                return new List<(string Chain, string Name, double Balance)>();
            }

            string Url = $"https://api.debank.com/portfolio/project_list?user_addr={Address.ToLower()}";
            var Responce = await Request.GetAsync(Url, this.DebankSigner.CreateHeaders(Url));

            var Protocols = (JsonConvert.DeserializeObject<IEnumerable<Protocol>>(GetResponceBody(Responce))
                ?? new List<Protocol>()).Where(Protocol => Protocol.Balance > 0.0d).Select(Protocol => (Chain: Protocol.Chain.ToUpper(), Name: Protocol.Name.Trim(), Protocol.Balance));

            if((Tries < 4 && Protocols.Any()) && (Protocols.Select(Protocol => Protocol.Name).Intersect(FakeProtocolsSignatures).Count() >= 2) || (Protocols.Select(Protocol => Protocol.Name).Intersect(FakeProtocolsStopSignatures).Count() >= 1))
            {
                return await GetProtocols(Address, ++Tries);
            }

            return Protocols;
        }

        public async Task<IEnumerable<string>> GetUsedChains(string Address)
        {
            string Url = $"https://api.debank.com/user/used_chains?id={Address.ToLower()}";

            return (JsonConvert.DeserializeObject<Chains>(GetResponceBody(await Request.GetAsync(Url, this.DebankSigner.CreateHeaders(Url))))
                ?? new()).UsedChains;
        }

        private static string GetResponceBody(string Json)
        {
            return JObject.Parse(Json).TryGetValue("data", out var Data) ? Data.ToString() : "[]";
        }
    }



    public class Debank : WalletCheckerModelHttp
    {
        public Debank(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.DEBANK, SelfCheck)
        {
            SelfCheckAddress = "0x47ac0fb4f2d84898e4d9e7b4dab3c24507a6d503";
        }

        protected override async Task<List<TokenCheckResult>> DeserializeTokenResponce(Wallet Wallet)
        {
            var Polly = Policy
           .Handle<Exception>()
           .RetryForeverAsync(async (Ex, Retry, Context) =>
           {
               //Console.WriteLine(JsonConvert.SerializeObject(Ex));
           });


            string Walleta;
            if (this.SelfCheck == true)
            {
                Walleta = this.SelfCheckAddress;
            } else
            {
                Walleta = Wallet.Address;
            }
            Tokens Tokens = new();

            var TokensResponce = (await Polly.ExecuteAsync(async () => await GetTokens(Walleta)));

            foreach (var Chain in TokensResponce)
            {
                foreach(var Token in Chain.Tokens)
                {
                    Tokens.Add(new($"{Token.Symbol}-{Chain.Chain}", Token.Balance, Token.Amount, Token.Contract, TokenType.ERC20));
                }
            }

            var ProtocolsResponce = (await Polly.ExecuteAsync(async () => await GetProtocols(Walleta)));

            foreach (var Protocol in ProtocolsResponce)
            {
                Tokens.Add(new($"{Protocol.Name}-{Protocol.Chain}", Protocol.Balance, 0, "Nothing", TokenType.PROTOCOL));
            }

            return Tokens;
        }

/*        private async Task<IEnumerable<(string Chain, IEnumerable<Token> Tokens)>> GetTokens(string Address, IEnumerable<string> UsedChains)
        {
           return await DebankWrapper.DebankClient.GetTokens(Address, UsedChains);
        }*/

        private async Task<IEnumerable<(string Chain, IEnumerable<FructoseCheckerV1.Debank.Types.Token> Tokens)>> GetTokens(string Address)
        {
            return await DebankWrapper.DebankClient.GetTokens(Address);
        }

        private async Task<IEnumerable<(string Chain, string Name, double Balance)>> GetProtocols(string Address)
        {
            return await DebankWrapper.DebankClient.GetProtocols(Address);
        }

        private async Task<IEnumerable<string>> GetUsedChains(string Address)
        {
            return await DebankWrapper.DebankClient.GetUsedChains(Address);
        }
    }
}
