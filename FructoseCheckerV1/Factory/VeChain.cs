using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using FructoseCheckerV1.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Numerics;

namespace FructoseCheckerV1.Factory
{
    public class VeChain : WalletCheckerModelHttp
    {
        public VeChain(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.VET, SelfCheck)
        {
            Url = "https://explore-mainnet.veblocks.net/accounts/{0}";
            SelfCheckAddress = "0x24e291723605ed77a71ea02e831716c7b87e051d";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=VETUSDT";
            ////*[@id="page_body"]/div[2]/div/div[2]/div[1]/div/div[2]/small/text()
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponse(GetUrl(Wallet));

                double Balance = (double)(BigInteger.Parse(new string(Object["balance"].ToString().Skip(2).ToArray()), System.Globalization.NumberStyles.HexNumber) / BigInteger.Parse("1000000000000000000"));
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
