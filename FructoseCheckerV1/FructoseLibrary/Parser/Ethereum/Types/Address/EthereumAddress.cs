using FructoseLib.Types.Ethereum.Enums;
using System.Collections.Generic;

namespace FructoseLib.Types.Ethereum.Types
{
    public struct EthereumAddress : IEthereumUnit
    {
        public EthereumAddress(string Address, (string Hash, long Line) Metadata)
        {
            this.Type = EthereumUnitType.Address;
            this.Value = Address;
            this.Accounts = Generate(this.Value);
            this.Metadata = Metadata;
        }

        public EthereumUnitType Type { get; init; }
        public string Value { get; init; }
        public IEnumerable<EthereumAccount> Accounts { get; init; }
        public (string Hash, long Line) Metadata { get; init; }
        private static IEnumerable<EthereumAccount> Generate(string Value)
        {
            yield return new(Value, "Nothing", $"Unknown");
        }
    }
}
