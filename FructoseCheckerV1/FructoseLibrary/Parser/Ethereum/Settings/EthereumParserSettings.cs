using System.Collections.Generic;


using FructoseLib.Types.Ethereum.Enums;

namespace FructoseLib.IO.Parser
{
    public sealed record DerivationPathOptions
    {
        public DerivationPathOptions(DerivationType Type, string Path, int Offset, int Depth)
        {
            this.Type = Type;
            this.Path = Path;
            this.Offset = Offset;
            this.Depth = Depth;
        }

        public DerivationPathOptions(DerivationType Type, string Path, bool Derive)
        {
            this.Type = Type;
            this.Path = Path;
            this.Offset = 0;
            this.Depth = 1;
        }

        public DerivationType Type { get; set; }
        public string Path { get; set; }
        public int Depth { get; set; }
        public int Offset { get; set; }
    }

    public sealed record EthereumParserSettings
    {
        public bool Antipublic { get; set; } = true;
        public bool DecompressArchives { get; set; } = true;
        public bool DeleteDecompressedAfter { get; set;} = true;
        public bool LoopMode { get; set; } = false;  
        public IEnumerable<InputNode> LoopModeInputNodes { get; set; } = new List<InputNode>();
        public IEnumerable<(DerivationType Type, string Path, int Depth, int Offset)> DerivationPathsOptions { get; set; } = new List<(DerivationType Type, string Path, int Depth, int Offset)>()
        {
            (DerivationType.Metamask, "m/44'/60'/0'/0/x", 0, 1),
            (DerivationType.Ledger, "m/44'/60'/x", 0, 1),
            (DerivationType.Atomic, "m/atomic(master key)", 0, 1)
        };

        public IEnumerable<(EthereumUnitType UnitType, bool Enabled)> SearchOptions { get; set; } = new List<(EthereumUnitType UnitType, bool Enabled)>()
        {
            (EthereumUnitType.Mnemonic, true),
            (EthereumUnitType.MnemonicFrench, true),
            (EthereumUnitType.MnemonicSpanish, true),
            (EthereumUnitType.MnemonicChinese, true),
            (EthereumUnitType.MnemonicChineseSimplified, true),
            (EthereumUnitType.MnemonicJapanese, true),
            (EthereumUnitType.MnemonicPortugueseBrazil, true),
            (EthereumUnitType.MnemonicCzech, true),
            (EthereumUnitType.PrivateKey, true),
            (EthereumUnitType.Address, true)
        };

        public IEnumerable<string> InExtensionsOptions { get; set; } = new string[]
{
            ".txt",
            ".json",
            ".xml",
            ".html",
            ".csv",
            ".tmp",
            ".cfg",
            ".config",
};
    }
}
