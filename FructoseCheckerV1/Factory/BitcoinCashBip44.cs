using FructoseCheckerV1.Models;
using HtmlAgilityPack;
using Nethereum.Util;
using Newtonsoft.Json.Linq;

using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    public class BitcoinCashBip44 : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 235.0d;

        public BitcoinCashBip44(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.BCH_BIP44, SelfCheck)
        {
            Url = "https://bitcoin-cash-node.atomicwallet.io/api/v2/xpub/{0}?&details=tokenBalances&tokens=used";
            SelfCheckAddress = "xpub6Bqp9aSbtJ9zamW5vJpYjFse1eGt9jae2JworzEHagRTCKxEJ3kyfhFvYuqEjYmi8nL5Z4q6Dw7WcYFhvDfnTszirMfy2dCUfotu4b2vTXu";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=BCHUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Object = await GetResponse(GetUrl(Wallet));
                if (Object.GetValue("balance") != null)
                {
                    double Balance = (double)new BigDecimal(BigInteger.Parse(Object.GetValue("balance").ToString()) / new BigDecimal(100000000));
                    return new Amount(Balance * await DeserializePriceResponce(), Balance);
                }
                else
                {
                    return new Amount(0.0d, 0.0d);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override async Task<double> DeserializePriceResponce()
        {
            if (PriceUpdatedAt.AddMinutes(5) < DateTime.Now)
            {
                try
                {
                    JObject Object = await GetResponse(PriceUrl);
                    PriceUpdatedAt = DateTime.Now;
                    return LastPrice = Convert.ToDouble(Object.GetValue("price"), new CultureInfo("ru-RU"));
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                return LastPrice;
            }

        }
    }
}
