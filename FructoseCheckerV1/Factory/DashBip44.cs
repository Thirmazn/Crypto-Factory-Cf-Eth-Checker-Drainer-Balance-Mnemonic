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
    class DashBip44 : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private static double LastPrice = 28.0d;
        public DashBip44(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.DASH_BIP44, SelfCheck)
        {
            Url = "https://dash.atomicwallet.io/api/v2/xpub/{0}?&details=tokenBalances&tokens=used";
            SelfCheckAddress = "xpub6CC7xhrrEtnsFsz17yyeRhC6sRRpTWAD1P7gjwGCNgJTfw7Xtc45n3d2coXGVgMjc4Kjx2KC49gmRXTG9b6NpGhZzsZAfAxa2EMqUQWsKrX";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=DASHUSDT";
        }


        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Object = await GetResponse(GetUrl(Wallet));
                double Balance = (double)new BigDecimal(BigInteger.Parse(Object.GetValue("balance").ToString()) / new BigDecimal(100000000));
                return new Amount(Balance * await DeserializePriceResponce(), Balance);
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
