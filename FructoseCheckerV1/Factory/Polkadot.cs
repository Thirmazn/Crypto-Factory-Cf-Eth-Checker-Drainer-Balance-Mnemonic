using FructoseCheckerV1.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{
    public class Account
    {
        [JsonProperty("account_display")]
        public AccountDisplay AccountDisplay { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("balance")]
        public string Balance { get; set; }

        [JsonProperty("balance_lock")]
        public string BalanceLock { get; set; }

        [JsonProperty("bonded")]
        public string Bonded { get; set; }

        [JsonProperty("count_extrinsic")]
        public int CountExtrinsic { get; set; }

        [JsonProperty("democracy_lock")]
        public string DemocracyLock { get; set; }

        [JsonProperty("election_lock")]
        public string ElectionLock { get; set; }

        [JsonProperty("evm_account")]
        public string EvmAccount { get; set; }

        [JsonProperty("is_council_member")]
        public bool IsCouncilMember { get; set; }

        [JsonProperty("is_erc20")]
        public bool IsErc20 { get; set; }

        [JsonProperty("is_erc721")]
        public bool IsErc721 { get; set; }

        [JsonProperty("is_evm_contract")]
        public bool IsEvmContract { get; set; }

        [JsonProperty("is_fellowship_member")]
        public bool IsFellowshipMember { get; set; }

        [JsonProperty("is_module_account")]
        public bool IsModuleAccount { get; set; }

        [JsonProperty("is_registrar")]
        public bool IsRegistrar { get; set; }

        [JsonProperty("is_techcomm_member")]
        public bool IsTechcommMember { get; set; }

        [JsonProperty("lock")]
        public string Lock { get; set; }

        [JsonProperty("nonce")]
        public int Nonce { get; set; }

        [JsonProperty("registrar_info")]
        public object RegistrarInfo { get; set; }

        [JsonProperty("reserved")]
        public string Reserved { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("staking_info")]
        public StakingInfo StakingInfo { get; set; }

        [JsonProperty("stash")]
        public string Stash { get; set; }

        [JsonProperty("substrate_account")]
        public object SubstrateAccount { get; set; }

        [JsonProperty("unbonding")]
        public string Unbonding { get; set; }

        [JsonProperty("vesting")]
        public object Vesting { get; set; }
    }

    public class AccountDisplay
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("merkle")]
        public Merkle Merkle { get; set; }
    }

    public class ControllerDisplay
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("merkle")]
        public Merkle Merkle { get; set; }
    }

    public class PolkadotData
    {
        [JsonProperty("account")]
        public Account Account { get; set; }
    }

    public class Merkle
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("tag_subtype")]
        public string TagSubtype { get; set; }

        [JsonProperty("tag_type")]
        public string TagType { get; set; }
    }

    public class PolkadotExplorer
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("generated_at")]
        public int GeneratedAt { get; set; }

        [JsonProperty("data")]
        public PolkadotData Data { get; set; }
    }

    public class StakingInfo
    {
        [JsonProperty("controller")]
        public string Controller { get; set; }

        [JsonProperty("controller_display")]
        public ControllerDisplay ControllerDisplay { get; set; }

        [JsonProperty("reward_account")]
        public string RewardAccount { get; set; }
    }


    public class Polkadot : WalletCheckerModelHttp
    {
        public Polkadot(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.DOT_SUBSTRATE, SelfCheck)
        {
            Url = "https://polkadot.webapi.subscan.io/api/v2/scan/search";
            SelfCheckAddress = "12xtAYsRUrmbniiWQqJtECiBQrMn8AypQcXhnQAc6RB6XkLW";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=DOTUSDT";
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                var Responce = await PostResponseString(GetUrl(Wallet), $"{{\"key\":\"{Wallet.Address}\",\"row\":1,\"page\":0}}");

                PolkadotExplorer PolkadotResponce = JsonConvert.DeserializeObject<PolkadotExplorer>(Responce);

                if(PolkadotResponce.Message != "Success" && PolkadotResponce.Code != 0)
                {
                    return new Amount(0, 0);
                }

                try
                {
                    double Balance = Convert.ToDouble(PolkadotResponce.Data.Account.Balance.Replace(".", ","), new CultureInfo("ru-RU"));
                    return new Amount(Balance * await DeserializePriceResponce(), Balance);
                } catch
                {
                    return new Amount(0, 0);
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
                return Convert.ToDouble(Object.GetValue("price"), new CultureInfo("ru-RU"));
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
