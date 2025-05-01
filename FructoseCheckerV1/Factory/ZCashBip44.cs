using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;

using Nethereum.Util;

using Newtonsoft.Json.Linq;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    class ZCashBip44 : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 27.0d;
        public ZCashBip44(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.ZEC_BIP44, SelfCheck)
        {
            Url = "https://zcash.atomicwallet.io/api/v2/xpub/{0}?&details=tokenBalances&tokens=used";
            SelfCheckAddress = "xpub6CHV9fnGrDqMWU5eCa1atg5SNaKbySCqaNS9XP9eCCUnqo4bWpeXmzaWq3def7DzLVkrCmr7nwpQvNDbXqN4XzHQB5isFJMR46WgHWtzoGN";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=ZECUSDT";
        }


        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Object = await GetResponse(GetUrl(Wallet));

                if(Object.GetValue("balance") != null) {
                    double Balance = (double)new BigDecimal(BigInteger.Parse(Object.GetValue("balance").ToString()) / new BigDecimal(100000000));
                    return new Amount(Balance * await DeserializePriceResponce(), Balance);
                } else
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








































































//@see_seed