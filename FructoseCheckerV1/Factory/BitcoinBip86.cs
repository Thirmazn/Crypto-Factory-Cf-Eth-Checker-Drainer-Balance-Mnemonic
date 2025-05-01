using FructoseCheckerV1.Models;
using FructoseCheckerV1.Utils;

using Nethereum.Util;

using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{

    class BitcoinBip86 : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 35000.0d;
        public BitcoinBip86(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.BTC_BIP86, SelfCheck)
        {
            Url = "https://bitcoin.atomicwallet.io/api/v2/xpub/tr({0})?&details=tokenBalances&tokens=used";
            SelfCheckAddress = "xpub6CfqbDUHiPj24nCo77YWhBgZKAi4CRhuDRBpQPKiK41ztcX5CY9GZXnaf7UAaGobDEe1eowJGDLWZMsR7bxreLV3tS7bNg7RxAsghmosikf";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
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
