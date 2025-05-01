using FructoseLib.Types.Ethereum.Enums;
using FructoseLib.Types.Ethereum.Types;
using System.Collections.Generic;

namespace FructoseLib.Types.Ethereum
{
    public interface IEthereumUnit
    {
        public EthereumUnitType Type { get; }
        public string Value { get; }
        public IEnumerable<EthereumAccount> Accounts { get; }
        public (string Hash, long Line) Metadata { get; }
    }
}
