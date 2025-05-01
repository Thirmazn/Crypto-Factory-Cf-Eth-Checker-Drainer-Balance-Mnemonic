using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using FructoseCheckerV1.Utils;

using FructoseLib.Extensions;

using HtmlAgilityPack;

using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using AirDropsCheckResult = System.Collections.Generic.List<FructoseCheckerV1.Models.AirDropCheckResult>;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using TokensCheckResult = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Models
{
    public enum CoinType { BTC_LEDGER, BTC_BIP84, BTC_BIP44, BTC_BIP49, BTC_BIP86, CARDANO_SHELLEY, BCH_BIP44, BCH_BIP49, BNB, ETC, DOT_SUBSTRATE, SOL, SOL_PHANTOM, TRX, ATOM, VET, LTC_BIP44, LTC_BIP49, LTC_BIP84, XRP, ALGO, XTZ, DASH_BIP44, DASH_BIP49, DOGE_BIP44, DOGE_BIP49, THETA, ZEC_BIP44, DEBANK, /*STARKNET_BRAAVOS, STARKNET_ARGENT, STARKNET_ARGENT_V0, APTOS, SUI*/ };
    public enum TokenType { ERC20, NFT, TRC20, TRC10, BNB, SOL, DEBANK, PROTOCOL, TOKEN };
    public enum WalletType { COINOMI };
    public static class CoinTypeExtensions
    {
        public static string GetScript(this CoinType Value) => Value switch
        {
            CoinType.BTC_LEDGER => "BitcoinLedger.py",
            CoinType.BTC_BIP84 => "BitcoinBip84.py",
            CoinType.BTC_BIP44 => "BitcoinBip44.py",
            CoinType.BTC_BIP49 => "BitcoinBip49.py",
            CoinType.BTC_BIP86 => "BitcoinBip86.py",
            CoinType.BCH_BIP44 => "BitcoinCashBip44.py",
            CoinType.BCH_BIP49 => "BitcoinCashBip49.py",
            CoinType.BNB => "BinanceChain.py",
            CoinType.DEBANK => "Debank.py",
            CoinType.ETC => "EthereumClassic.py",
            CoinType.ALGO => "Algorand.py",
            CoinType.XTZ => "Tezos.py",
            CoinType.DASH_BIP44 => "DashBip44.py",
            CoinType.DASH_BIP49 => "DashBip49.py",
            CoinType.DOGE_BIP44 => "DogecoinBip44.py",
            CoinType.DOGE_BIP49 => "DogecoinBip49.py",
            CoinType.THETA => "Theta.py",
            CoinType.ZEC_BIP44 => "ZCashBip44.py",
            CoinType.DOT_SUBSTRATE => "Polkadot.py",
            CoinType.SOL => "Solana.py",
            CoinType.SOL_PHANTOM => "SolanaPhantom.py",
            CoinType.TRX => "Tron.py",
            CoinType.ATOM => "Cosmos.py",
            CoinType.VET => "VeChain.py",
            CoinType.LTC_BIP44 => "LitecoinBip44.py",
            CoinType.LTC_BIP49 => "LitecoinBip49.py",
            CoinType.LTC_BIP84 => "LitecoinBip84.py",
            CoinType.XRP => "Ripple.py",
            _ => throw new NotSupportedException(),
        };
        public static string GetExplorerFormated(this CoinType Value) => Value switch
        {
            CoinType.BTC_LEDGER => "https://btc1.trezor.io/address/{0}",
            CoinType.BTC_BIP84 => "https://btc1.trezor.io/xpub/{0}",
            CoinType.BTC_BIP44 => "https://btc1.trezor.io/xpub/{0}",
            CoinType.BTC_BIP49 => "https://btc1.trezor.io/xpub/{0}",
            CoinType.BTC_BIP86 => "https://btc1.trezor.io/xpub/tr\\({0}\\)",
            CoinType.BCH_BIP44 => "https://bch1.trezor.io/xpub/{0}",
            CoinType.BCH_BIP49 => "https://bch1.trezor.io/xpub/{0}",
            CoinType.BNB => "https://explorer.bnbchain.org/address/{0}",
            CoinType.DEBANK => "https://debank.com/profile/{0}",
            CoinType.ETC => "https://etc.blockscout.com/address/{0}",
            CoinType.ALGO => "https://explorer.perawallet.app/address/{0}/",
            CoinType.XTZ => "https://tzkt.io/{0}/balances/",
            CoinType.DASH_BIP44 => "https://dash1.trezor.io/xpub/{0}",
            CoinType.DASH_BIP49 => "https://dash1.trezor.io/xpub/{0}",
            CoinType.DOGE_BIP44 => "https://doge1.trezor.io/xpub/{0}",
            CoinType.DOGE_BIP49 => "https://doge1.trezor.io/xpub/{0}",
            CoinType.THETA => "https://explorer.thetatoken.org/account/{0}",
            CoinType.ZEC_BIP44 => "https://zcash.atomicwallet.io/api/v2/xpub/{0}?&details=tokenBalances&tokens=used",
            CoinType.DOT_SUBSTRATE => "https://polkadot.subscan.io/account/{0}",
            CoinType.SOL => "https://app.step.finance/en/dashboard?watching={0}",
            CoinType.SOL_PHANTOM => "https://app.step.finance/en/dashboard?watching={0}",
            CoinType.TRX => "https://tronscan.org/#/address/{0}",
            CoinType.ATOM => "https://www.mintscan.io/cosmos/address/{0}",
            CoinType.VET => "https://explore.vechain.org/accounts/{0}/",
            CoinType.LTC_BIP44 => "https://ltc1.trezor.io/xpub/{0}",
            CoinType.LTC_BIP49 => "https://ltc1.trezor.io/xpub/{0}",
            CoinType.LTC_BIP84 => "https://ltc1.trezor.io/xpub/{0}",
            CoinType.XRP => "https://xrpscan.com/account/{0}",
            _ => throw new NotSupportedException(),
        };
    }

    public abstract class WalletCheckerModelBase
    {
        public WalletCheckerModelBase(ref Python Python, CoinType CoinType, bool SelfCheck, WalletType WalletType)
        {
            this.SelfCheck = SelfCheck;
            this.Python = Python;
            this.CoinType = CoinType;
            this.WalletType = WalletType;
            this.Wallets = new();
        }

        protected Python Python { get; set; }
        protected WalletType WalletType { get; init; }
        protected CoinType CoinType { get; set; }
        protected NetworkSettignsStorage NetworkSettigns { get; set; }
        protected Wallet SelfCheckWallet { get { return new(SelfCheckAddress, "null"); } }
        protected List<Wallet> Wallets { get; set; }
        protected string Url { init { UrlTemplate = value; } }
        private string UrlTemplate { get; init; }
        protected string GetUrl(Wallet Wallet)
        {
            return SelfCheck == false ? string.Format(UrlTemplate, Wallet.Address) : string.Format(UrlTemplate, SelfCheckAddress);
        }
        protected string GetTokensUrl(Wallet Wallet)
        {
            return SelfCheck == false ? string.Format(TokensUrlTemplate, Wallet.Address) : string.Format(TokensUrlTemplate, SelfCheckAddress);
        }
        protected string GetNftUrl(Wallet Wallet)
        {
            return SelfCheck == false ? string.Format(NftUrlTemplate, Wallet.Address) : string.Format(NftUrlTemplate, SelfCheckAddress);
        }
        protected virtual string TokensUrl { init { TokensUrlTemplate = value; } }
        protected virtual string NftUrl { init { NftUrlTemplate = value; } }
        private string NftUrlTemplate { get; set; }
        private string TokensUrlTemplate { get; set; }
        protected virtual string AirDropsUrl { init { AirDropsUrlTemplate = value; } }
        private string AirDropsUrlTemplate { get; set; }
        protected bool SelfCheck { get; set; }
        protected string SelfCheckAddress { get; init; }

        public WalletCheckerModelBase ConfigureNetwork(NetworkSettignsStorage NetworkSettigns)
        {
            this.NetworkSettigns = NetworkSettigns;
            return this;
        }
        public virtual WalletCheckerModelBase GetAccounts(string Mnemonic, int Count = 1)
        {
            if (Count < 1)
            {
                Count = 1;
            }

        Try:
            try
            {
                if(CoinType == CoinType.DOT_SUBSTRATE)
                {
                    string Return = Python.Execute(CoinType.GetScript(),
                        new[] { $"\"{Mnemonic}\"", Count.ToString() });
                    foreach (var Row in Return.Split('\n').Where(Row => Row.Length > 0))
                    {
                        Wallets.Add(new(Row));
                    }
                    return this;
                } else
                {
                    var SeedBytesHexString = BitConverter.ToString(new NBitcoin.Mnemonic(Mnemonic).DeriveSeed()).Replace("-", "").ToLower();

                    string Return = Python.Execute(CoinType.GetScript(),
                        new[] { SeedBytesHexString, Count.ToString() });
                    foreach (var Row in Return.Split('\n').Where(Row => Row.Length > 0))
                    {
                        Wallets.Add(new(Row));
                    }
                    return this;
                }

            }
            catch (Exception Ex)
            {
                goto Try;
            }

        }

        public virtual WalletCheckerModelBase GetAccountsFromPrivateKey(string PrivateKey)
        {
            Wallets.Add(new(Nethereum.Signer.EthECKey.GetPublicAddress(PrivateKey), PrivateKey));
            return this;
        }

        public virtual WalletCheckerModelBase GetAccountsFromAddress(string Address)
        {
            Wallets.Add(new(Address, "Nothing"));
            return this;
        }

        public async Task<IEnumerable<CoinCheckResult>> GetBalances()
        {
            List<Task<CoinCheckResult>> Tasks = new();

            if (SelfCheck == false)
            {
                foreach (var Wallet in Wallets)
                {
                    Tasks.Add(GetBalance(Wallet));
                }
            }
            else
            {
                Tasks.Add(GetBalance(SelfCheckWallet));
            }


            return await Task.WhenAll(Tasks);
        }
        protected abstract Task<CoinCheckResult> GetBalance(Wallet Wallet);
        public abstract Task<CoinCheckResult> GetSelfCheckBalance();
    }
    public abstract class WalletChekerModelXpath : WalletCheckerModelBase
    {
        public WalletChekerModelXpath(ref Python Python, CoinType CoinType, bool SelfCheck)
            : base(ref Python, CoinType, SelfCheck, WalletType.COINOMI)
        {
        }
        protected string AirDropUrl { get; set; }
        protected string XpathPrice { get; init; }
        protected string XpathBalance { get; init; }
        protected string XpathToken { get; init; }
        protected string XpathTokenName { get; init; }
        protected string XpathTokenBalance { get; init; }
        protected string XpathTokenPrice { get; init; }
        protected string XpathTokenContract { get; init; }
        protected string XpathNFT { get; init; }
        protected string XpathNFTName { get; init; }
        protected string XpathNFTBalance { get; init; }
        protected string XpathNFTPrice { get; init; }
        protected string XpathNFTContract { get; init; }

        public sealed override async Task<CoinCheckResult> GetSelfCheckBalance()
        {
            int Tries = 0;
        Try:
            try
            {
                Tries++;
                var FirstTask = ParseCoinHtml(SelfCheckWallet);
                var SecondTask = ParseTokenHtml(SelfCheckWallet);

                await Task.WhenAll(FirstTask, SecondTask);

                var Result = new CoinCheckResult(CoinType, FirstTask.Result.First, FirstTask.Result.Second, SelfCheckWallet.Address, SelfCheckWallet.PrivateKey);
                Result.Tokens = SecondTask.Result;
                return Result;
            }
            catch (Exception Ex)
            {
                if (Tries < NetworkSettigns.RetryCountIfError)
                {
                    await Task.Delay(200);
                    goto Try;
                }
                else
                {
                    return new CoinCheckResult(CoinType, 0.0, 0.0, SelfCheckWallet.Address, SelfCheckWallet.PrivateKey, true, Ex.Message);
                }
            }
        }
        protected sealed override async Task<CoinCheckResult> GetBalance(Wallet Wallet)
        {
            int Tries = 0;
        Try:
            try
            {
                Tries++;
                var FirstTask = ParseCoinHtml(Wallet);
                var SecondTask = ParseTokenHtml(Wallet);

                await Task.WhenAll(FirstTask, SecondTask);

                var Result = new CoinCheckResult(CoinType, FirstTask.Result.First, FirstTask.Result.Second, Wallet.Address, Wallet.PrivateKey);
                Result.Tokens = SecondTask.Result;
                return Result;
            }
            catch (Exception Ex)
            {
                if (Tries < NetworkSettigns.RetryCountIfError)
                {
                    await Task.Delay(1000);
                    goto Try;
                }
                else
                {
                    return new CoinCheckResult(CoinType, 0.0, 0.0, Wallet.Address, Wallet.PrivateKey, true, Ex.Message);
                }
            }
        }
        protected virtual async Task<TokensCheckResult> ParseTokenHtml(Wallet Wallet) => new();
        protected virtual async Task<Amount> ParseCoinHtml(Wallet Wallet) => new(0.0, 0.0);
        protected async Task<HtmlDocument> GetHtml(string Url)
        {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();
            try
            {
                Document.LoadHtml(await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random()));
                return Document;
            }
            catch (Exception Ex)
            {
                throw new WalletCheckerGetHtmlException(Url, Ex.Message);
            }
        }
    }

    public abstract class WalletCheckerModelHttp : WalletCheckerModelBase
    {
        public WalletCheckerModelHttp(ref Python Python, CoinType CoinType, bool SelfCheck)
            : base(ref Python, CoinType, SelfCheck, WalletType.COINOMI)
        {
        }

        protected string PriceUrl { get; set; }
        protected string AirDropUrl { get; set; }

        public sealed override async Task<CoinCheckResult> GetSelfCheckBalance()
        {
            int Tries = 0;
        Try:
            try
            {
                Tries++;
                var FirstTask = DeserializeCoinResponce(SelfCheckWallet);
                var SecondTask = DeserializeTokenResponce(SelfCheckWallet);
                //var ThirdTask = DeserializeAirDropResponce(SelfCheckWallet);

                await Task.WhenAll(FirstTask, SecondTask);

                var Result = new CoinCheckResult(CoinType, FirstTask.Result.First, FirstTask.Result.Second, SelfCheckWallet.Address, SelfCheckWallet.PrivateKey);
                Result.Tokens = SecondTask.Result;
                //Result.AirDrops = ThirdTask.Result;
                return Result;
            }
            catch (WalletCheckerGetResponceNotFoundException)
            {
                return new CoinCheckResult(CoinType, 0.0, 0.0, SelfCheckWallet.Address, SelfCheckWallet.PrivateKey, false, String.Empty);
            }
            catch (Exception Ex)
            {

                if (Tries < NetworkSettigns.RetryCountIfError)
                {
                    await Task.Delay(200);
                    goto Try;
                } else
                {
                    return new CoinCheckResult(CoinType, 0.0, 0.0, SelfCheckWallet.Address, SelfCheckWallet.PrivateKey, true, Ex.Message);
                }
            }
        }
        protected sealed override async Task<CoinCheckResult> GetBalance(Wallet Wallet)
        {
            int Tries = 0;
        Try:
            try
            {
                Tries++;
                var FirstTask = DeserializeCoinResponce(Wallet);
                var SecondTask = DeserializeTokenResponce(Wallet);

                await Task.WhenAll(FirstTask, SecondTask);

                var Result = new CoinCheckResult(CoinType, FirstTask.Result.First, FirstTask.Result.Second, Wallet.Address, Wallet.PrivateKey);
                Result.Tokens = SecondTask.Result;
                return Result;
            }
            catch (WalletCheckerGetResponceNotFoundException)
            {
                return new CoinCheckResult(CoinType, 0.0, 0.0, Wallet.Address, Wallet.PrivateKey, false, string.Empty);
            }
            catch (Exception Ex)
            {
                if (Tries < NetworkSettigns.RetryCountIfError)
                {
                    await Task.Delay(3000);
                    goto Try;
                }
                else
                {
                    await Task.Delay(1000);
                    return new CoinCheckResult(CoinType, 0.0, 0.0, Wallet.Address, Wallet.PrivateKey, true, Ex.Message);
                }
            }
        }

        protected virtual async Task<double> DeserializePriceResponce() => 0.0;
        protected virtual async Task<AirDropsCheckResult> DeserializeAirDropResponce(Wallet Wallet) => new();
        protected virtual async Task<TokensCheckResult> DeserializeTokenResponce(Wallet Wallet) => new();
        protected virtual async Task<Amount> DeserializeCoinResponce(Wallet Wallet) => new(0.0, 0.0);
        protected async Task<JObject> GetResponse(string Url)
        {
            try
            {
                return JObject.Parse(await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random()));
            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<JObject> GetEventResponse(string Url, string EventName, Dictionary<string, string> Headers)
        {

            try
            {
                return JObject.Parse(await Network.GetEventAsync(Url, EventName, Headers, Proxy: NetworkSettigns.Proxy.Random()));
            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> GetEventResponseString(string Url, string EventName, Dictionary<string, string> Headers)
        {

            try
            {
                return await Network.GetEventAsync(Url, EventName, Headers, Proxy: NetworkSettigns.Proxy.Random());
            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<JObject> GetResponseWithHeaders(string Url, Dictionary<string, string> Headers)
        {
            try
            {
                return JObject.Parse(await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random(), Headers: Headers));

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<JObject> GetResponseWithHeadersSecure(string Url, Dictionary<string, string> Headers)
        {
            try
            {
                return JObject.Parse(await Network.GetAsyncSecure(Url, Proxy: NetworkSettigns.Proxy.Random(), Headers: Headers));

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<JObject> PostResponse(string Url, string Data)
        {
            try
            {
                return JObject.Parse(await Network.PostAsync(Url, Data, Proxy: NetworkSettigns.Proxy.Random()));

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> PostResponseString(string Url, string Data)
        {
            try
            {
                return await Network.PostAsync(Url, Data, Proxy: NetworkSettigns.Proxy.Random());

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> GetResponseStringWithHeaders(string Url, Dictionary<string, string> Headers)
        {
            try
            {
                return await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random(), Headers: Headers);

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> GetResponseStringWithHeadersDisableTLSCheck(string Url, Dictionary<string, string> Headers)
        {
            try
            {
                return await Network.GetAsyncDisableTLSCheck(Url, Proxy: NetworkSettigns.Proxy.Random(), Headers: Headers);

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> GetResponseStringWithHeadersSecure(string Url, Dictionary<string, string> Headers)
        {
            try
            {
                return await Network.GetAsyncSecure(Url, Proxy: NetworkSettigns.Proxy.Random(), Headers: Headers);

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
        protected async Task<string> GetResponseString(string Url)
        {
            try
            {
                return await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random());
            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
    }
    public abstract class WalletCheckerModelСombine : WalletCheckerModelBase
    {
        public WalletCheckerModelСombine(ref Python Python, CoinType CoinType, bool SelfCheck)
            : base(ref Python, CoinType, SelfCheck, WalletType.COINOMI)
        {
        }
        protected string PriceUrl { get; set; }
        protected string AirDropUrl { get; set; }
        protected string XpathPrice { get; init; }
        protected string XpathBalance { get; init; }
        protected string XpathToken { get; init; }
        protected string XpathTokenName { get; init; }
        protected string XpathTokenBalance { get; init; }
        protected string XpathTokenPrice { get; init; }
        protected string XpathTokenContract { get; init; }
        protected string XpathNFT { get; init; }
        protected string XpathNFTName { get; init; }
        protected string XpathNFTBalance { get; init; }
        protected string XpathNFTPrice { get; init; }
        protected string XpathNFTContract { get; init; }

        public abstract override Task<CoinCheckResult> GetSelfCheckBalance();
        protected abstract override Task<CoinCheckResult> GetBalance(Wallet Wallet);

        protected virtual async Task<AirDropsCheckResult> ParseAirDropHtml(Wallet Wallet) => new();
        protected virtual async Task<TokensCheckResult> ParseTokenHtml(Wallet Wallet) => new();
        protected virtual async Task<Amount> ParseCoinHtml(Wallet Wallet) => new(0.0, 0.0);
        protected async Task<HtmlDocument> GetHtml(string Url)
        {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();

            try
            {
                Document.LoadHtml(await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random()));
                return Document;
            }
            catch (Exception Ex)
            {
                throw new WalletCheckerGetHtmlException(Url, Ex.Message);
            }
        }

        protected virtual async Task<double> DeserializePriceResponce() => 0.0;
        protected virtual async Task<AirDropsCheckResult> DeserializeAirDropResponce(Wallet Wallet) => new();
        protected virtual async Task<TokensCheckResult> DeserializeTokenResponce(Wallet Wallet) => new();
        protected virtual async Task<Amount> DeserializeCoinResponce(Wallet Wallet) => new(0.0, 0.0);
        protected async Task<JObject> GetResponse(string Url)
        {
            try
            {
                return JObject.Parse(await Network.GetAsync(Url, Proxy: NetworkSettigns.Proxy.Random()));

            }
            catch (HttpRequestException Ex)
            {
                if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new WalletCheckerGetResponceNotFoundException(Url, Ex.Message);
                }
                else if (Ex != null && Ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(1000);
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
                else
                {
                    throw new WalletCheckerGetResponceException(Url, Ex.Message);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException Ex)
            {
                throw new WalletCheckerJsonException(Url, Ex.Message);
            }
        }
    }

    public struct Wallet
    {
        public string Address { get; set; }
        public string PrivateKey { get; set; }

        public Wallet(string Init)
        {
            Address = Init.Split(':')[0];
            PrivateKey = Init.Split(':')[1];
        }
        public Wallet(string Address, string PrivateKey)
        {
            this.Address = Address;
            this.PrivateKey = PrivateKey;
        }
    }

    public struct CoinCheckResult
    {
        public CoinCheckResult(CoinType CoinType, double Price, double Balance, string Address, string PrivateKey, bool Error = false, string ErrorMessage = "")
        {
            this.Error = Error;
            this.ErrorMessage = ErrorMessage;
            this.CoinType = CoinType;
            this.Balance = Balance;
            this.Price = Price;
            this.Address = Address;
            this.PrivateKey = PrivateKey;
            this.Tokens = new();
            this.Txs = 0;
        }
        public bool Error { get; private set; }
        public string ErrorMessage { get; private set; }
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CoinType CoinType { get; private set; }
        public double Balance { get; private set; }
        public double Price { get; private set; }
        public int Txs { get; private set; }
        public string Address { get; private set; }
        public string PrivateKey { get; private set; }
        public TokensCheckResult Tokens { get; set; }
        public bool ShouldSerializeError()
        {
            return Error;
        }
        public bool ShouldSerializeErrorMessage()
        {
            return Error;
        }
        public bool ShouldSerializeTxs()
        {
            if(Txs > 0)
            {
                return true;
            } else
            {
                return false;
            }
        }
        public bool ShouldSerializeTokens()
        {
            if(Tokens.Count > 0)
            {
                return true;
            } else
            {
                return false;
            }
        }
    }

    public struct TokenCheckResult
    {
        public TokenCheckResult(string Name, double Price, double Balance, string Contract, TokenType Token)
        {
            this.Name = Name;
            this.Price = Price;
            this.Balance = Balance;
            TokenType = Token;
            this.Contract = Contract;
        }
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public TokenType TokenType { get; private set; }
        public string Name { get; private set; }
        public double Price { get; private set; }
        public double Balance { get; private set; }
        public string Contract { get; private set; }
    }

    public struct AirDropCheckResult
    {
        public AirDropCheckResult(TokenType TokenType, bool Claimed, string Name, double Price)
        {
            this.TokenType = TokenType;
            this.Claimed = Claimed;
            this.Name = Name;
            this.Price = Price;
        }

        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public TokenType TokenType { get; private set; }
        public bool Claimed { get; private init; }
        public string Name { get; private set; }
        public double Price { get; private set; }
    }

    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T First, U Second)
        {
            this.First = First;
            this.Second = Second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };

    public class WalletCheckerException : Exception
    {
        public WalletCheckerException()
            : base("Unexpected wallet checker module error.")
        {

        }

        public WalletCheckerException(string Message)
            : base(Message)
        {
        }

        public WalletCheckerException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class WalletCheckerXpathException : WalletCheckerException
    {
        public WalletCheckerXpathException()
            : base("Unable to apply xpath expression to current document.")
        {

        }

        public WalletCheckerXpathException(string Message)
            : base(Message)
        {
        }

        public WalletCheckerXpathException(string Url, string Reason)
            : base($"Unable to apply xpath expression to document from: {Url} - {Reason}.")
        {
        }

        public WalletCheckerXpathException(string Url, Exception Inner)
            : base($"Unable to apply xpath expression to document from: {Url}.")
        {
        }
    }
    public class WalletCheckerJsonException : WalletCheckerException
    {
        public WalletCheckerJsonException()
            : base("Unable to parse json object.")
        {

        }

        public WalletCheckerJsonException(string Url, string Reason)
            : base($"Unable to parse json object from: {Url} - {Reason}.")
        {
        }

        public WalletCheckerJsonException(string Url, Exception Inner)
            : base($"Unable to parse json object from: {Url}.")
        {
        }
    }
    public class WalletCheckerGetResponceException : WalletCheckerException
    {
        public WalletCheckerGetResponceException()
            : base($"Unable to get responce.")
        {
        }

        public WalletCheckerGetResponceException(string Url, string Reason)
            : base($"Unable to get responce from: {Url} - {Reason}.")
        {
        }

        public WalletCheckerGetResponceException(string Url, Exception Inner)
            : base($"Unable to get responce from: {Url}.")
        {
        }
    }
    public class WalletCheckerGetResponceNotFoundException : WalletCheckerException
    {
        public WalletCheckerGetResponceNotFoundException()
            : base($"Unable to get responce(404).")
        {
        }

        public WalletCheckerGetResponceNotFoundException(string Url, string Reason)
            : base($"Unable to get responce(404) from: {Url} - {Reason}.")
        {
        }

        public WalletCheckerGetResponceNotFoundException(string Url, Exception Inner)
            : base($"Unable to get responce(404) from: {Url}.")
        {
        }
    }
    public class WalletCheckerServiceUnavailableException : WalletCheckerException
    {
        public WalletCheckerServiceUnavailableException()
            : base($"Service is temporarily unavailable.")
        {

        }

        public WalletCheckerServiceUnavailableException(string Url)
            : base($"Service is temporarily unavailable: {Url}.")
        {
        }

        public WalletCheckerServiceUnavailableException(string Url, Exception Inner)
            : base($"Service is temporarily unavailable: {Url}.")
        {
        }
    }
    public class WalletCheckerGetHtmlException : WalletCheckerException
    {
        public WalletCheckerGetHtmlException()
            : base($"Unable to get html document.")
        {
        }

        public WalletCheckerGetHtmlException(string Url, string Reason)
            : base($"Unable to get html document from: {Url} - {Reason}.")
        {
        }

        public WalletCheckerGetHtmlException(string Url, Exception Inner)
            : base($"Unable to get html document from: {Url}.")
        {
        }
    }
}
