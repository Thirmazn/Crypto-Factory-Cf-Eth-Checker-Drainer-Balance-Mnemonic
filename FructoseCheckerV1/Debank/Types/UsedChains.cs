using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FructoseCheckerV1.Debank.Types
{
    internal class Chains
    {
        [JsonProperty("chains")]
        public List<string> UsedChains { get; private set; }

        public Chains()
        {
            this.UsedChains = new();
        }
    }
}
