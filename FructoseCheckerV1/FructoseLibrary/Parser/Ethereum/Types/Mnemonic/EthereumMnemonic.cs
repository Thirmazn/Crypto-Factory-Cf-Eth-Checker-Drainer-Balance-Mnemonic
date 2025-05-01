using System.Collections.Generic;

using FructoseLib.Types.Ethereum.Enums;

using NBitcoin;

using Nethereum.HdWallet;
using Nethereum.Web3.Accounts;

namespace FructoseLib.Types.Ethereum.Types
{
    public struct EthereumMnemonic : IEthereumUnit
    {
        public EthereumMnemonic(string Mnemonic, EthereumUnitType Type, IEnumerable<(DerivationType Type, string Path, int Depth, int Offset)> DerivationOptions, (string Hash, long Line) Metadata)
        {
            this.Type = Type;
            this.Value = Mnemonic;
            this.Accounts = Derive(this.Value, DerivationOptions);
            this.Metadata = Metadata;
        }

        public EthereumUnitType Type { get; init; }
        public string Value { get; private set; }
        public IEnumerable<EthereumAccount> Accounts { get; init; }
        public (string Hash, long Line) Metadata { get; init; }
        private static IEnumerable<EthereumAccount> Derive(string Mnemonic, IEnumerable<(DerivationType Type, string Path, int Depth, int Offset)> Derivations)
        {
            foreach (var (Type, Path, Depth, Offset) in Derivations)
            {
                if (Type == DerivationType.Metamask || Type == DerivationType.Ledger) 
                {
                    if (Depth > 0)
                    {
                        var Wallet = new Wallet(Mnemonic, string.Empty, Path);

                        for (int Index = 0; Index < Depth; Index++)
                        {
                            var Account = Wallet.GetAccount(Offset + Index);
                            yield return new(Account.Address, Account.PrivateKey, Path.Replace("x", (Offset + Index).ToString()));
                        }
                    }
                } else if(Type == DerivationType.Atomic && Depth > 0)
                {
                    var Key = $"0x{new Mnemonic(Mnemonic).DeriveExtKey().PrivateKey.ToHex()}";
                    yield return new(Nethereum.Signer.EthECKey.GetPublicAddress(Key), Key, Path);
                }
            }
        }
    }

    public struct EthereumAccount
    {
        public EthereumAccount(string Address, string PrivateKey, string DerivationPath)
        {
            this.Address = Address.ToLower();
            this.PrivateKey = PrivateKey.Length == 64 ? PrivateKey.Replace("0x", "0x00") : PrivateKey;
            this.DerivationPath = DerivationPath;
        }
        public string Address { get; init; }
        public string PrivateKey { get; init; }
        public string DerivationPath { get; init; }

        public Nethereum.Web3.Accounts.Account ToNethereumAccount(int ChainId)
        {
            return new(this.PrivateKey, ChainId);
        }
    }
}
