using FructoseCheckerV1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    class Tezos : WalletCheckerModelHttp
    {
        public Tezos(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.XTZ, SelfCheck)
        {
            Url = "https://back.tzkt.io/v1/accounts/{0}?legacy=false\r\n";
            SelfCheckAddress = "tz1VLEq23Acf9Zp6xi3CxC4LNHuVjKf6LyZv";
            PriceUrl = "https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=USD";
            //https://api.tzstats.com/explorer/account/tz1VLEq23Acf9Zp6xi3CxC4LNHuVjKf6LyZv?meta=1
            //n_tx
        }


        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponse(GetUrl(Wallet));
                if (Object.GetValue("balance") != null)
                {
                    double Balance = Convert.ToUInt64(Object.GetValue("balance"), new CultureInfo("ru-RU")) / 1000000d;
                    return new(Balance * await DeserializePriceResponce(), Balance);
                } else
                {
                    return new(0.0d, 0.0d);
                }

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
                return Convert.ToDouble(Object.GetValue("USD"), new CultureInfo("ru-RU"));
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
