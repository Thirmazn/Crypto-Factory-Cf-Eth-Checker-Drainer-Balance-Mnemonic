using FructoseCheckerV1.Models;
using HtmlAgilityPack;

using Nethereum.Util;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    class EthereumClassic : WalletCheckerModelHttp
    {
        public EthereumClassic(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.ETC, SelfCheck)
        {
            Url = "https://etc.blockscout.com/api/v2/addresses/{0}";
            SelfCheckAddress = "0x45d022c169c1198c29F9CBe58C666fc8D1Bb41f1";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=ETCUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponse(GetUrl(Wallet));
                double Balance = ((double)(BigDecimal.Parse(Object["coin_balance"].ToString()) / BigInteger.Pow(10, 18)));
                return new Amount(Balance * await DeserializePriceResponce(), Balance);
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
