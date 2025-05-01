using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FructoseCheckerV1.Debank.Types;

using Newtonsoft.Json;

namespace FructoseCheckerV1.Debank.Types
{

    public class Balance
    {
        [JsonProperty("user")]
        public User User { get; set; }
    }

    public class Stats
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("top_coins")]
        public List<Token> Coins { get; set; }

        [JsonProperty("top_collections")]
        public List<Collection> Collections { get; set; }

        [JsonProperty("top_protocols")]
        public List<ProtocolV2> Protocols { get; set; }

        [JsonProperty("top_tokens")]
        public List<Token> Tokens { get; set; }

        [JsonProperty("usd_value")]
        public double UsdValue { get; set; }
    }

    public class Coin
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("logo_url")]
        public string LogoUrl { get; set; }

        [JsonProperty("percent")]
        public double Percent { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("usd_value")]
        public double UsdValue { get; set; }
    }

    public class Collection
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("logo_url")]
        public string LogoUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }
    }

    public class ProtocolV2
    {
        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("logo_url")]
        public string LogoUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("percent")]
        public double Percent { get; set; }

        [JsonProperty("usd_value")]
        public double UsdValue { get; set; }
    }

    public class TokenV2
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("logo_url")]
        public string LogoUrl { get; set; }

        [JsonProperty("percent")]
        public double Percent { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("usd_value")]
        public double UsdValue { get; set; }
    }

    public class User
    {
        [JsonProperty("stats")]
        public Stats Stats { get; set; }
    }

}
