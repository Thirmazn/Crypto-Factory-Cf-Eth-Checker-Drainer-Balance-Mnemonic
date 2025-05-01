using FructoseCheckerV1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    class Algorand : WalletCheckerModelHttp
    {
        public Algorand(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.ALGO, SelfCheck)
        {
            Url = "https://mainnet-api.algonode.cloud/v2/accounts/{0}?format=json&exclude=all";
            SelfCheckAddress = "737777777777777777777777777777777777777777777777777UFEJ2CI";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=ALGOUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponseWithHeadersSecure(GetUrl(Wallet), GetHeaders());
                double Balance = Convert.ToDouble(Object["amount"], new CultureInfo("ru-RU")) / 1000000;
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

        private static Dictionary<string, string> GetHeaders()
        {
            return new()
            {
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36" }
            };
        }
    }
}
