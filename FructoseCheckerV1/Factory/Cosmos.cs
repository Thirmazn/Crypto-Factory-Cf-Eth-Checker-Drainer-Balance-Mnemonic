using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;

using FructoseCheckerV1.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FructoseCheckerV1.Debank.Types;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{

    public class CosmosData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("tokens")]
        public List<Asset> Tokens { get; set; }

        [JsonProperty("totalValue")]
        public double TotalValue { get; set; }

    }

    public class SolscanResponce
    {
        [JsonProperty("data")]
        public CosmosData Data { get; set; }
    }

    public class Asset
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("decimalsAmount")]
        public string DecimalsAmount { get; set; }

        [JsonProperty("price")]
        public double? Price { get; set; }

        [JsonProperty("totalValue")]
        public double? TotalValue { get; set; }

        [JsonProperty("token")]
        public Token2 Info { get; set; }
    }

    public class Token2
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("chainId")]
        public int ChainId { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("coingeckoId")]
        public string CoingeckoId { get; set; }
    }


    public class Cosmos : WalletCheckerModelHttp
    {
        public Cosmos(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.ATOM, SelfCheck)
        {
            Url = "https://api.de.fi/v1/account/sse?addresses[]={0}&chains[]=25&events[]=balances&events[]=returns";
            SelfCheckAddress = "cosmos1x54ltnyg88k0ejmk8ytwrhd3ltm84xehrnlslf";
        }

        protected override async Task<List<TokenCheckResult>> DeserializeTokenResponce(Wallet Wallet)
        {
            List<TokenCheckResult> Tokens = new();

            try
            {
                var Responce = await GetEventResponseString(GetUrl(Wallet), "balances", null);
                //Console.WriteLine(Responce);
                if(JsonConvert.DeserializeObject<SolscanResponce>(Responce.Replace(Wallet.Address, "data")).Data is null || JsonConvert.DeserializeObject<SolscanResponce>(Responce.Replace(Wallet.Address, "data")).Data.Tokens is null)
                {
                    return Tokens;
                }

                foreach(var Token in JsonConvert.DeserializeObject<SolscanResponce>(Responce.Replace(Wallet.Address, "data")).Data.Tokens)
                {
                    Tokens.Add(new(Token.Info.DisplayName, Token.TotalValue ?? 0d, Convert.ToDouble((Token.DecimalsAmount ?? string.Empty).Replace(".", ",")), Token.Info.Address, TokenType.TOKEN));
                }

                return Tokens;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
