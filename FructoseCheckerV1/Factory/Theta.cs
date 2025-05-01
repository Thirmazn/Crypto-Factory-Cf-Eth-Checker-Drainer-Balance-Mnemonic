using FructoseCheckerV1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    class Theta : WalletCheckerModelHttp
    {
        public Theta(ref Python Python, bool SelfCheck = false)
           : base(ref Python, CoinType.THETA, SelfCheck)
        {
            Url = "https://explorer.thetatoken.org:8443/api/account/update/{0}";
            SelfCheckAddress = "0x157a3026c9daa62ae4428757b5c96b4b567ae716";
            PriceUrl = "https://explorer.thetatoken.org:8443/api/price/all";
        }


        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponse(GetUrl(Wallet));
                double Balance = Convert.ToDouble(Object.GetValue("body")["balance"]["thetawei"], new CultureInfo("ru-RU")) / 1000000000000000000;
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
                return Convert.ToDouble(Object.GetValue("body").Last["price"], new CultureInfo("ru-RU"));
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
