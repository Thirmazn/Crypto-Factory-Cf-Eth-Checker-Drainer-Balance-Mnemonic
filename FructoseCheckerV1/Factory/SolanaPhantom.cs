using FructoseCheckerV1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using Tokens = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Factory
{
    public class SolanaPhantom : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 220.0d;
        public SolanaPhantom(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.SOL_PHANTOM, SelfCheck)
        {
            Url = "https://107.155.116.187/v2/account?address={0}";
            TokensUrl = "https://107.155.116.187/v2/account/tokens?address={0}";
            SelfCheckAddress = "LA1NEzryoih6CQW3gwQqJQffK2mKgnXcjSQZSRpM3wc";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Responce = JsonConvert.DeserializeObject<AccountResponse>(await GetResponseStringWithHeadersDisableTLSCheck(GetUrl(Wallet), GetHeaders()));

                if (Responce.Data.Lamports != null && Responce.Data.Lamports > 0)
                {
                    return new((Responce.Data.Lamports / 1000000000.0d) * Responce.Metadata.Tokens.First().Value.PriceUsdt, Responce.Data.Lamports / 1000000000.0d);
                }
                else
                {
                    return new(0.0, 0.0);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override async Task<Tokens> DeserializeTokenResponce(Wallet Wallet)
        {
            try
            {
                Tokens Tokens = new();

                var Responce = JsonConvert.DeserializeObject<SolanaTokensResponse>(await GetResponseStringWithHeadersDisableTLSCheck(GetTokensUrl(Wallet), GetHeaders()));


                if (Responce.Data != null && Responce.Data.Tokens != null && Responce.Data.Tokens.Count() > 0)
                {
                    foreach (var Token in Responce.Data.Tokens.Where(Token => Token.Value > 0.0d && Token.TokenSymbol != null))
                    {
                        Tokens.Add(new(Token.TokenSymbol, Token.Value, Token.Balance, Token.TokenAddress, TokenType.SOL));
                    }
                }
                else
                {
                    return new();
                }

                return Tokens.ToList();
            }
            catch (Exception)
            {
                return new();
            }
        }

        private static Dictionary<string, string> GetHeaders()
        {
            return new()
            {
                { "Host", "api-v2.solscan.io" }
            };
        }
    }
}
