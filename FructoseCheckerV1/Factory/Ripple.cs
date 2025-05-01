using FructoseCheckerV1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{

    public class Ripple : WalletCheckerModelHttp
    {
        public Ripple(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.XRP, SelfCheck)
        {
            Url = "https://ripple.a.exodus.io/wallet/v1";
            SelfCheckAddress = "rpKpv6ktQNKA33K8A2z6Sjip2vvozH9gJj";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=XRPUSDT";

        }

        private static Dictionary<string, string> Headers = new()
        {
            { "origin", "https://xrpscan.com" },
            { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36" }
        };

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await PostResponse(GetUrl(Wallet), $"{{\r\n   \"method\" : \"account_info\",\r\n   \"params\" : [\r\n      {{\r\n         \"account\" : \"{Wallet.Address}\",\r\n         \"api_version\" : 1,\r\n         \"ledger_index\" : \"validated\"\r\n      }}\r\n   ]\r\n}}");

                if (Object.ToString().Contains("actNotFound") )
                {
                    return new(0.0, 0.0);
                }

                if(Convert.ToInt32(Object.GetValue("result")["account_data"]["OwnerCount"]) > 1)
                {
                    return new(0.0, 0.0);
                }


                double Balance = Convert.ToDouble(Convert.ToDecimal(Object.GetValue("result")["account_data"]["Balance"]) / 1000000m, new CultureInfo("ru-RU"));

                //Token check future
                /*string TokenUrl = "https://api.xrpscan.com/api/v1/account/{0}";
                JObject TokensObject = GetResponse("https://api.xrpscan.com/api/v1/account/rsC4v4YRWrqWvWGvFjDfZLeb9CrHVuz3Rg/assets");

                foreach (var Token in TokensObject.Values())
                {
                    Console.WriteLine(String.Format(TokenUrl, Token["counterparty"]));
                    JObject TokenObject = GetResponse(String.Format(TokenUrl, Token["counterparty"]));
                    Balance += Convert.ToDouble(TokenObject.GetValue("xrpBalance"));
                }*/

                return new(Balance * await DeserializePriceResponce(), Balance);
            }
            catch (Exception)
            {

                throw;
            }
        }

        protected override async Task<double> DeserializePriceResponce()
        {
            try
            {
                JObject Object = await GetResponse(PriceUrl);
                return Convert.ToDouble(Object.GetValue("price"), new CultureInfo("ru-RU"));
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
