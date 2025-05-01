using FructoseLib.Types.Ethereum.Enums;
using FructoseLib.Utils;

namespace FructoseLib.IO.Parser
{
    public sealed record EthereumSearchTypeStorage
    {
        public EthereumSearchTypeStorage(EthereumUnitType Type)
        {
            this.Type = Type;
        }

        public EthereumUnitType Type { get; init; }
        public long Valid { get; private set; }
        public long Invalid { get; private set; }
        public long Duplicates { get; private set; }
        public long Private { get; private set; }
        public long Public { get; private set; }
        public long Total { get => Valid + Invalid; }

        public void SetPrivate(long Value) => Private = Value;
        public void SetPublic(long Value) => Public = Value;
        public void SetDuplicates(long Value) => Duplicates = Value;
        public void SetValid(long Value) => Valid = Value;
        public void SetInvalid(long Value) => Invalid = Value;
    }
}
