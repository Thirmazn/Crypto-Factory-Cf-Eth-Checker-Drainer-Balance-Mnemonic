using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using Tokens = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Factory
{

    #region Tokens
    public class SolanaTokensResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public TokenData Data { get; set; }
    }

    public class TokenData
    {
        [JsonProperty("data_type")]
        public string DataType { get; set; }

        [JsonProperty("tokens")]
        public List<Token> Tokens { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class Token
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("tokenAddress")]
        public string TokenAddress { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("reputation")]
        public string Reputation { get; set; }

        [JsonProperty("priceUsdt")]
        public double PriceUsdt { get; set; }

        [JsonProperty("tokenName")]
        public string TokenName { get; set; }

        [JsonProperty("tokenSymbol")]
        public string TokenSymbol { get; set; }

        [JsonProperty("tokenIcon")]
        public string TokenIcon { get; set; }

        [JsonProperty("balance")]
        public double Balance { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }


    #endregion

    #region Account
    public class AccountResponse
    {
        [JsonProperty("data")]
        public AccountData Data { get; set; }

        [JsonProperty("metadata")]
        public AccountResponceMetadata Metadata { get; set; }
    }

    // Класс для раздела "data"
    public class AccountData
    {
        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("lamports")]
        public long Lamports { get; set; }

        [JsonProperty("ownerProgram")]
        public string OwnerProgram { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("rentEpoch")]
        public string RentEpoch { get; set; } // Используем string для больших чисел

        [JsonProperty("executable")]
        public bool Executable { get; set; }

        [JsonProperty("isOnCurve")]
        public bool IsOnCurve { get; set; }

        [JsonProperty("space")]
        public int Space { get; set; }
    }

    // Класс для раздела "metadata"
    public class AccountResponceMetadata
    {
        [JsonProperty("tokens")]
        public Dictionary<string, AccountResponceTokenMetadata> Tokens { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, object> Tags { get; set; }

        [JsonProperty("programs")]
        public Dictionary<string, object> Programs { get; set; }

        [JsonProperty("nftCollections")]
        public Dictionary<string, object> NftCollections { get; set; }

        [JsonProperty("nftMarketplaces")]
        public Dictionary<string, object> NftMarketplaces { get; set; }
    }

    // Класс для метаданных токенов
    public class AccountResponceTokenMetadata
    {
        [JsonProperty("token_address")]
        public string TokenAddress { get; set; }

        [JsonProperty("token_name")]
        public string TokenName { get; set; }

        [JsonProperty("token_symbol")]
        public string TokenSymbol { get; set; }

        [JsonProperty("token_icon")]
        public string TokenIcon { get; set; }

        [JsonProperty("token_decimals")]
        public int TokenDecimals { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("holder")]
        public long Holder { get; set; }

        [JsonProperty("price_usdt")]
        public double PriceUsdt { get; set; }

        [JsonProperty("reputation")]
        public string Reputation { get; set; }

        [JsonProperty("is_show_value")]
        public bool IsShowValue { get; set; }

        [JsonProperty("onchain_extensions")]
        public object OnchainExtensions { get; set; }

        [JsonProperty("real_circulating_supply")]
        public string RealCirculatingSupply { get; set; }

        [JsonProperty("is_calculate_on_portfolio")]
        public bool IsCalculateOnPortfolio { get; set; }
    }


    #endregion



    public class Solana : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 220.0d;
        public Solana(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.SOL, SelfCheck)
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
                    foreach(var Token in Responce.Data.Tokens.Where(Token => Token.Value > 0.0d && Token.TokenSymbol != null))
                    {
                        Tokens.Add(new(Token.TokenSymbol, Token.Value, Token.Balance, Token.TokenAddress, TokenType.SOL));
                    }
                } else
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
