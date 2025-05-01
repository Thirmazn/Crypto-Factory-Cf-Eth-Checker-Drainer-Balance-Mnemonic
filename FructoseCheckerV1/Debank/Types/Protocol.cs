using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FructoseCheckerV1.Debank.Types
{
    public class Holdings
    {
        [JsonProperty("stats")]
        private Balances Stats { get; set; }
        public double Balance { get => this.Stats.TotalUsdValue ?? 0.0d; }
    }

    public class Protocol
    {
        [JsonProperty("chain")]
        public string Chain { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("id")]
        public string Id { get; private set; }


        [JsonProperty("portfolio_item_list")]
        private List<Holdings> Holdings { get; set; }

        public double Balance { get => Math.Round(this.Holdings.Sum(Holding => Holding.Balance), 2); }
    }

    public class Balances
    {
        [JsonProperty("asset_usd_value")]
        public double? TotalUsdValue { get; private set; }
    }
}
