using FructoseLib.Types.Ethereum.Enums;
using System.Collections.Generic;

namespace FructoseLib.Types.Ethereum.Types
{
    public struct EthereumPrivateKey : IEthereumUnit
    {
        public EthereumPrivateKey(string PrivateKey, (string Hash, long Line) Metadata)
        {
            this.Type = EthereumUnitType.PrivateKey;
            this.Value = "0x" + PrivateKey;
            this.Accounts = Generate(this.Value);
            this.Metadata = Metadata;
        }

        public EthereumUnitType Type { get; init; }
        public string Value { get; init; }
        public IEnumerable<EthereumAccount> Accounts { get; init; }
        public (string Hash, long Line) Metadata { get; init; }
        private static IEnumerable<EthereumAccount> Generate(string Key)
        {
            yield return new(Nethereum.Signer.EthECKey.GetPublicAddress(Key), Key, $"Unknown") ;
        }
    }
}
