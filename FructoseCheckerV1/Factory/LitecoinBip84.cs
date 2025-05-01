using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;
using FructoseCheckerV1.Utils;

using Nethereum.Util;

using Newtonsoft.Json.Linq;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    public class LitecoinBip84 : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 68.0d;
        public LitecoinBip84(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.LTC_BIP84, SelfCheck)
        {
            Url = "https://litecoin.atomicwallet.io/api/v2/xpub/{0}?&details=tokenBalances&tokens=used";
            SelfCheckAddress = "zpub6r5M8Y6QbZv9HJtbAiCMHK1obrehWcssby3f7LKCyevTJWEGF8H4tfFsx2FwDgqMuyC8UDgpF4vFv6bKLk6A7c2ZbfL3QawyMnUYYQtByri";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=LTCUSDT";
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
