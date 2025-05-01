using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FructoseCheckerV1.Debank.Types
{
    public class Token
    {
        [JsonProperty("amount")]
        public double Amount { get; private set; }

        [JsonProperty("price")]
        private double? Price { get; set; }

/*        [JsonProperty("chain_id")]
        private string Chain { get; set; }*/

        [JsonProperty("chain")]
        public string Chain { get; set; }

        [JsonProperty("id")]
        public string Contract { get; private set; }

        [JsonProperty("symbol")]
        public string Symbol { get; private set; }

/*        [JsonProperty("usd_value")]
        public double UsdValue { get; set; }*/

        public double Balance { get => Math.Round(Amount * Price ?? 0.0d, 2); }
        //public double Balance { get => UsdValue; }

    }
}
