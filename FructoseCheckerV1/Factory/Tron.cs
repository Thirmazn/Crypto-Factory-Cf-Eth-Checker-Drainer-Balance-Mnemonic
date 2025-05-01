using FructoseCheckerV1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using Tokens = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Factory
{
    public struct TronResponce
    {
        [JsonProperty("total")]
        public string Total { get; set; }
        [JsonProperty("data")]
        public IList<TronToken> Data { get; set; }
    }

    public struct TronToken
    {
        [JsonProperty("tokenName")]
        public string Name { get; set; }
        [JsonProperty("tokenId")]
        public string Contract { get; set; }
        [JsonProperty("quantity")]
        public double Balance { get; set; }
        [JsonProperty("tokenPriceInTrx")]
        public double PriceInTrx { get; set; }
    }


    public class OwnerPermissionKey
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }

    public class OwnerPermission
    {
        [JsonProperty("keys")]
        public List<OwnerPermissionKey> Keys { get; set; }
    }

    public class OwnerPermissionRoot
    {
        [JsonProperty("ownerPermission")]
        public OwnerPermission OwnerPermission { get; set; }
    }


    public class Tron : WalletCheckerModelHttp
    {
        public Tron(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.TRX, SelfCheck)
        {
            Url = "https://apilist.tronscan.org/api/account/tokens?address={0}&start=0&limit=20&hidden=0&show=0&sortType=0&sortBy=0";
            TokensUrl = "https://apilist.tronscan.org/api/account/tokens?address={0}&start=0&limit=20&hidden=0&show=0&sortType=0&sortBy=0";
            SelfCheckAddress = "TU4vEruvZwLLkSfV9bNw12EJTPvNr7Pvaa";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=TRXUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                OwnerPermissionRoot ownerPermissionRoot = JsonConvert.DeserializeObject<OwnerPermissionRoot>(await GetResponseString($"https://apilist.tronscanapi.com/api/accountv2?address={Wallet.Address}"));

                if (ownerPermissionRoot.OwnerPermission != null && ownerPermissionRoot.OwnerPermission.Keys != null && ownerPermissionRoot.OwnerPermission.Keys.First().Address.Equals(Wallet.Address, StringComparison.OrdinalIgnoreCase))
                {
                    JObject Object = await GetResponse(GetUrl(Wallet));

                    IList<TronToken> TronWallet = JsonConvert.DeserializeObject<TronResponce>(Object.ToString()).Data;

                    if (TronWallet != null && TronWallet[0].PriceInTrx != null)
                    {
                        return new(TronWallet[0].Balance * TronWallet[0].PriceInTrx * await DeserializePriceResponce(), TronWallet[0].Balance);
                    }
                    else
                    {
                        return new(0.0, 0.0);
                    }
                }
                else
                {
                    return new(0.0, 0.0);
                }
            }
            catch (Exception Ex)
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

        protected override async Task<Tokens> DeserializeTokenResponce(Wallet Wallet)
        {
            try
            {
                Tokens Tokens = new();
                OwnerPermissionRoot ownerPermissionRoot = JsonConvert.DeserializeObject<OwnerPermissionRoot>(await GetResponseString($"https://apilist.tronscanapi.com/api/accountv2?address={Wallet.Address}"));

                if (ownerPermissionRoot.OwnerPermission != null && ownerPermissionRoot.OwnerPermission.Keys != null && ownerPermissionRoot.OwnerPermission.Keys.First().Address.Equals(Wallet.Address, StringComparison.OrdinalIgnoreCase))
                {


                    JObject Object = await GetResponse(GetTokensUrl(Wallet));

                    IList<TronToken> TokensCollection = JsonConvert.DeserializeObject<TronResponce>(Object.ToString()).Data;
                    TokensCollection.RemoveAt(0);

                    double TrxPrice = await DeserializePriceResponce();

                    foreach (var Token in TokensCollection)
                    {
                        Tokens.Add(new TokenCheckResult(Token.Name, (Token.Balance * Math.Round(Token.PriceInTrx, 10)) * TrxPrice, Token.Balance, Token.Contract, TokenType.TRC20));
                    }


                }
                return Tokens;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
