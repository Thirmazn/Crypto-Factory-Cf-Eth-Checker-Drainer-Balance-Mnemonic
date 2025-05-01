using CardanoSharp.Wallet;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using FructoseCheckerV1.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;

using System;
using System.Globalization;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    public class AccountDetailsData
    {
        [JsonProperty("address")]
        public string Address;

        [JsonProperty("tx")]
        public int Tx;

        [JsonProperty("token")]
        public int Token;

        [JsonProperty("first_tx_hash")]
        public string FirstTxHash;

        [JsonProperty("first_tx_time")]
        public int FirstTxTime;

        [JsonProperty("last_tx_hash")]
        public string LastTxHash;

        [JsonProperty("last_tx_time")]
        public int LastTxTime;

        [JsonProperty("account_hash")]
        public string AccountHash;

        [JsonProperty("reward_address")]
        public string RewardAddress;

        [JsonProperty("account_balance")]
        public string AccountBalance;

        [JsonProperty("account_reward_balance")]
        public string AccountRewardBalance;

        [JsonProperty("account_total_reward_amount")]
        public string AccountTotalRewardAmount;

        [JsonProperty("account_active_stake")]
        public string AccountActiveStake;

        [JsonProperty("account_snapshot_stake")]
        public string AccountSnapshotStake;

        [JsonProperty("registered_stake_key")]
        public bool RegisteredStakeKey;

        [JsonProperty("pool_hash")]
        public string PoolHash;

        [JsonProperty("pool_bech32")]
        public string PoolBech32;

        [JsonProperty("pool_name")]
        public string PoolName;

        [JsonProperty("pool_ticker")]
        public string PoolTicker;

        [JsonProperty("active_pool_hash")]
        public string ActivePoolHash;

        [JsonProperty("active_pool_bech32")]
        public string ActivePoolBech32;

        [JsonProperty("active_pool_name")]
        public string ActivePoolName;

        [JsonProperty("active_pool_ticker")]
        public string ActivePoolTicker;

        [JsonProperty("snapshot_pool_hash")]
        public string SnapshotPoolHash;

        [JsonProperty("snapshot_pool_bech32")]
        public string SnapshotPoolBech32;

        [JsonProperty("snapshot_pool_name")]
        public string SnapshotPoolName;

        [JsonProperty("snapshot_pool_ticker")]
        public string SnapshotPoolTicker;

        [JsonProperty("balance")]
        public string Balance;

        [JsonProperty("hash")]
        public string Hash;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("type_int")]
        public int TypeInt;
    }

    public class Result
    {
        [JsonProperty("data")]
        public AccountDetailsData Data;

        [JsonProperty("code")]
        public int Code;
    }

    public class Cardano : WalletCheckerModelHttp
    {
        public override WalletCheckerModelBase GetAccounts(string Mnemonic, int Count = 1)
        {

            Mnemonic mnemonic = new(Mnemonic, new NBitcoin.Mnemonic(Mnemonic).DeriveSeed());

            PrivateKey rootKey = mnemonic.GetRootKey();
            
            for (int i = 0; i < Count; i++)
            {
                string paymentPath = $"m/1852'/1815'/0'/0/{i}";
                PrivateKey paymentPrv = rootKey.Derive(paymentPath);
                PublicKey paymentPub = paymentPrv.GetPublicKey(false);

                string stakePath = $"m/1852'/1815'/0'/2/{i}";
                PrivateKey stakePrv = rootKey.Derive(stakePath);
                PublicKey stakePub = stakePrv.GetPublicKey(false);

                IAddressService addressService = new AddressService();

                Address baseAddr = addressService.GetAddress(paymentPub, stakePub, NetworkType.Mainnet, AddressType.Base);

                Wallets.Add(new($"{baseAddr.ToString()}:Nothing"));
            }

            for (int i = 0; i < Count; i++)
            {
                string paymentPath = $"m/44'/1815'/0'/0/{i}";
                PrivateKey paymentPrv = rootKey.Derive(paymentPath);
                PublicKey paymentPub = paymentPrv.GetPublicKey(false);

                string stakePath = $"m/44'/1815'/0'/2/{i}";
                PrivateKey stakePrv = rootKey.Derive(stakePath);
                PublicKey stakePub = stakePrv.GetPublicKey(false);

                IAddressService addressService = new AddressService();

                Address baseAddr = addressService.GetAddress(paymentPub, stakePub, NetworkType.Mainnet, AddressType.Base);

                Wallets.Add(new($"{baseAddr.ToString()}:Nothing"));
            }
            return this;
        }
        public Cardano(ref Python Python, bool SelfCheck = false)
            : base(ref Python, FructoseCheckerV1.Models.CoinType.CARDANO_SHELLEY, SelfCheck)
        {
            Url = "https://adastat.net/api/rest/v1/addresses/{0}.json?currency=usd";
            SelfCheckAddress = "addr1qynm6l6rkjpv0l8nh8496vjj2wd4j7z27hkpnyqv785evrjv0s0sslprxxqsp4mw35alagcvjcyn0szrnz8p645zgn2qrh0e5s";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=ADAUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Responce = await GetResponseString(GetUrl(Wallet));
                var Data = JsonConvert.DeserializeObject<Result>(Responce).Data;
                double Balance = (Convert.ToInt64(Data.AccountBalance) / 1000000) + (Convert.ToInt64(Data.AccountRewardBalance) / 1000000) + (Convert.ToInt64(Data.AccountActiveStake) / 1000000);
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

