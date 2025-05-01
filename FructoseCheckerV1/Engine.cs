using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FructoseCheckerV1.Factory;
using FructoseCheckerV1.Models;

using FructoseLib.CLI.Visual.Dynamic;

using Newtonsoft.Json;

using CoinCheckResults = System.Collections.Generic.List<FructoseCheckerV1.Models.CoinCheckResult>;
using Console = Colorful.Console;

namespace FructoseCheckerV1
{

    public struct TextReport
    {
        public TextReport(double Balance, string CoinCheckResults, string MnemonicOrPrivateKey, int NftCount, List<CoinType> Services, double BalanceInProtocols, List<string> Protocols)
        {
            this.Balance = Balance;
            this.CoinCheckResults = CoinCheckResults;
            this.MnemonicOrPrivateKeyOrAddress = MnemonicOrPrivateKey;
            this.NftCount = NftCount;
            this.BalanceInProtocols = BalanceInProtocols;
            this.Services = Services;
            this.Protocols = Protocols;
        }

        public double Balance { get; init; }
        public string CoinCheckResults { get; init; }
        public string MnemonicOrPrivateKeyOrAddress { get; init; }
        public int NftCount { get; init; }
        public double BalanceInProtocols { get; init; }
        public List<CoinType> Services { get; init; }
        public List<string> Protocols { get; init; }
    }

    public struct JsonReport
    {
        public JsonReport(double Balance, CoinCheckResults CoinCheckResults, string Mnemonic)
        {
            this.Balance = Balance;
            this.CoinCheckResults = CoinCheckResults;
            this.Mnemonic = Mnemonic;
        }

        public double Balance { get; init; }
        public CoinCheckResults CoinCheckResults { get; init; }
        public string Mnemonic { get; init; }
    }

    public struct ConsoleReport
    {
        public ConsoleReport(double Balance, CoinCheckResults CoinCheckResults, string MnemonicOrPrivateKeyOrAddress, bool SelfCheck)
        {
            this.Balance = Balance;
            this.CoinCheckResults = CoinCheckResults;
            this.MnemonicOrPrivateKeyOrAddress = MnemonicOrPrivateKeyOrAddress;
            this.SelfCheck = SelfCheck;
        }
        public bool SelfCheck { get; init; }
        public double Balance { get; init; }
        public CoinCheckResults CoinCheckResults { get; init; }
        public string MnemonicOrPrivateKeyOrAddress { get; init; }
    }

    public struct TelegramReport
    {
        public TelegramReport(double Balance, string Mnemonic)
        {
            this.Balance = Balance;
            this.Mnemonic = Mnemonic;
        }

        public double Balance { get; init; }
        public string Mnemonic { get; init; }
    }

    public class Engine
    {
        private EthereumClassic EthereumClassic { get; init; }
        private FructoseCheckerV1.Factory.BitcoinLedger BitcoinLedger { get; init; }
        private FructoseCheckerV1.Factory.BitcoinBip84 BitcoinBip84 { get; init; }
        private FructoseCheckerV1.Factory.BitcoinBip44 BitcoinBip44 { get; init; }
        private FructoseCheckerV1.Factory.BitcoinBip49 BitcoinBip49 { get; init; }
        private FructoseCheckerV1.Factory.BitcoinBip86 BitcoinBip86 { get; init; }
        private FructoseCheckerV1.Factory.Cardano Cardano { get; init; }
        private BinanceChain BinanceChain { get; init; }
        private Polkadot Polkadot { get; init; }
        private BitcoinCashBip44 BitcoinCashBip44 { get; init; }
        private BitcoinCashBip49 BitcoinCashBip49 { get; init; }
        private Tron Tron { get; init; }
        private Cosmos Cosmos { get; init; }
        private VeChain VeChain { get; init; }
        private LitecoinBip44 LitecoinBip44 { get; init; }
        private LitecoinBip49 LitecoinBip49 { get; init; }
        private LitecoinBip84 LitecoinBip84 { get; init; }
        private Solana Solana { get; init; }
        private SolanaPhantom SolanaPhantom { get; init; }
        private Ripple Ripple { get; init; }
        private Algorand Algorand { get; init; }
        private Tezos Tezos { get; init; }
        private DashBip44 DashBip44 { get; init; }
        private DashBip49 DashBip49 { get; init; }
        private DogecoinBip44 DogecoinBip44 { get; init; }
        private DogecoinBip49 DogecoinBip49 { get; init; }
        private Theta Theta { get; init; }
        private ZCashBip44 ZCashBip44 { get; init; }
        private FructoseCheckerV1.Factory.Debank Debank { get; init; }

        private string Mnemonic { get; set; }
        private string PrivateKey { get; set; }
        private string Address { get; set; }
        private bool SelfCheck { get; init; }
        private IEnumerable<CoinTypeProperty> CoinsProperties { get; init; }
        private CoinCheckResults CoinCheckResults { get;  set; }
        
        public Engine(ref Python Python, IEnumerable<CoinTypeProperty> CoinsProperties, NetworkSettignsStorage NetworkSettigns, bool SelfCheck = false)
        {
            this.CoinCheckResults = new();
            this.CoinsProperties = CoinsProperties;
            this.SelfCheck = SelfCheck;

            this.EthereumClassic = new EthereumClassic(ref Python, SelfCheck);

            this.BitcoinLedger = new FructoseCheckerV1.Factory.BitcoinLedger(ref Python, SelfCheck);
            this.BitcoinBip84 = new FructoseCheckerV1.Factory.BitcoinBip84(ref Python, SelfCheck);
            this.BitcoinBip44 = new FructoseCheckerV1.Factory.BitcoinBip44(ref Python, SelfCheck);
            this.BitcoinBip49 = new FructoseCheckerV1.Factory.BitcoinBip49(ref Python, SelfCheck);
            this.BitcoinBip86 = new FructoseCheckerV1.Factory.BitcoinBip86(ref Python, SelfCheck);

            this.Cardano = new FructoseCheckerV1.Factory.Cardano(ref Python, SelfCheck);
            this.Polkadot = new Polkadot(ref Python, SelfCheck);
            this.BinanceChain = new BinanceChain(ref Python, SelfCheck);
            this.Debank = new FructoseCheckerV1.Factory.Debank(ref Python, SelfCheck);
            this.BitcoinCashBip44 = new BitcoinCashBip44(ref Python, SelfCheck);
            this.BitcoinCashBip49 = new BitcoinCashBip49(ref Python, SelfCheck);
            this.Cosmos = new Cosmos(ref Python, SelfCheck);
            this.Tron = new Tron(ref Python, SelfCheck);
            this.VeChain = new VeChain(ref Python, SelfCheck);
            this.LitecoinBip44 = new LitecoinBip44(ref Python, SelfCheck);
            this.LitecoinBip49 = new LitecoinBip49(ref Python, SelfCheck);
            this.LitecoinBip84 = new LitecoinBip84(ref Python, SelfCheck);
            this.Solana = new Solana(ref Python, SelfCheck);
            this.SolanaPhantom = new SolanaPhantom(ref Python, SelfCheck);
            this.Ripple = new Ripple(ref Python, SelfCheck);
            this.Algorand = new Algorand(ref Python, SelfCheck);
            this.Tezos = new Tezos(ref Python, SelfCheck);
            this.DashBip44 = new DashBip44(ref Python, SelfCheck);
            this.DashBip49 = new DashBip49(ref Python, SelfCheck);
            this.DogecoinBip44 = new DogecoinBip44(ref Python, SelfCheck);
            this.DogecoinBip49 = new DogecoinBip49(ref Python, SelfCheck);
            this.Theta = new Theta(ref Python, SelfCheck);
            this.ZCashBip44 = new ZCashBip44(ref Python, SelfCheck);

            this.BitcoinLedger.ConfigureNetwork(NetworkSettigns);
            this.EthereumClassic.ConfigureNetwork(NetworkSettigns);
            this.BitcoinBip84.ConfigureNetwork(NetworkSettigns);
            this.BitcoinBip44.ConfigureNetwork(NetworkSettigns);
            this.BitcoinBip49.ConfigureNetwork(NetworkSettigns);
            this.BitcoinBip86.ConfigureNetwork(NetworkSettigns);
            this.Polkadot.ConfigureNetwork(NetworkSettigns);
            this.BinanceChain.ConfigureNetwork(NetworkSettigns);
            this.BitcoinCashBip44.ConfigureNetwork(NetworkSettigns);
            this.BitcoinCashBip49.ConfigureNetwork(NetworkSettigns);

            this.Cosmos.ConfigureNetwork(NetworkSettigns);
            this.Tron.ConfigureNetwork(NetworkSettigns);
            this.VeChain.ConfigureNetwork(NetworkSettigns);
            this.LitecoinBip44.ConfigureNetwork(NetworkSettigns);
            this.LitecoinBip49.ConfigureNetwork(NetworkSettigns);
            this.LitecoinBip84.ConfigureNetwork(NetworkSettigns);
            this.Debank.ConfigureNetwork(NetworkSettigns);
            this.Solana.ConfigureNetwork(NetworkSettigns);
            this.SolanaPhantom.ConfigureNetwork(NetworkSettigns);
            this.Ripple.ConfigureNetwork(NetworkSettigns);
            this.Algorand.ConfigureNetwork(NetworkSettigns);
            this.Tezos.ConfigureNetwork(NetworkSettigns);
            this.DashBip44.ConfigureNetwork(NetworkSettigns);
            this.DashBip49.ConfigureNetwork(NetworkSettigns);
            this.DogecoinBip44.ConfigureNetwork(NetworkSettigns);
            this.DogecoinBip49.ConfigureNetwork(NetworkSettigns);
            this.Theta.ConfigureNetwork(NetworkSettigns);
            this.ZCashBip44.ConfigureNetwork(NetworkSettigns);
            this.Cardano.ConfigureNetwork(NetworkSettigns);
        }

        public async Task Check(string Mnemonic)
        {
            this.Mnemonic = Mnemonic;

            if (this.SelfCheck)
            {
                return;
            }

            List<Task<IEnumerable<CoinCheckResult>>> Tasks = new();

            foreach (var CoinProperty in CoinsProperties)
            {
                switch (CoinProperty.CoinType)
                {
                    case CoinType.ALGO:
                        Tasks.Add(Algorand.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.ETC:
                        Tasks.Add(EthereumClassic.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BTC_BIP84:
                        Tasks.Add(BitcoinBip84.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BTC_BIP44:
                        Tasks.Add(BitcoinBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BTC_BIP49:
                        Tasks.Add(BitcoinBip49.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BTC_BIP86:
                        Tasks.Add(BitcoinBip86.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.CARDANO_SHELLEY:
                        Tasks.Add(Cardano.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BCH_BIP44:
                        Tasks.Add(BitcoinCashBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BCH_BIP49:
                        Tasks.Add(BitcoinCashBip49.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.LTC_BIP44:
                        Tasks.Add(LitecoinBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.LTC_BIP49:
                        Tasks.Add(LitecoinBip49.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.LTC_BIP84:
                        Tasks.Add(LitecoinBip84.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.SOL:
                        Tasks.Add(Solana.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DEBANK:
                        Tasks.Add(Debank.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.SOL_PHANTOM:
                        Tasks.Add(SolanaPhantom.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.ATOM:
                        Tasks.Add(Cosmos.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DOT_SUBSTRATE:
                        Tasks.Add(Polkadot.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.XRP:
                        Tasks.Add(Ripple.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.XTZ:
                        Tasks.Add(Tezos.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DASH_BIP44:
                        Tasks.Add(DashBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DASH_BIP49:
                        Tasks.Add(DashBip49.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DOGE_BIP44:
                        Tasks.Add(DogecoinBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.DOGE_BIP49:
                        Tasks.Add(DogecoinBip49.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.THETA:
                        Tasks.Add(Theta.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.ZEC_BIP44:
                        Tasks.Add(ZCashBip44.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.VET:
                        Tasks.Add(VeChain.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.TRX:
                        Tasks.Add(Tron.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    case CoinType.BNB:
                        Tasks.Add(BinanceChain.GetAccounts(this.Mnemonic, CoinProperty.Depth).GetBalances());
                        break;
                    default:
                        break;
                }
            }
            await Task.WhenAll(Tasks); 
            Tasks.ToList().ForEach(Task => CoinCheckResults.AddRange(Task.Result));
        }

        public async Task CheckPrivateKey(string PrivateKey)
        {
            this.PrivateKey = PrivateKey;

            if (this.SelfCheck)
            {
                return;
            }

            CoinCheckResults.AddRange(await Debank.GetAccountsFromPrivateKey(this.PrivateKey).GetBalances());
        }

        public async Task CheckAddress(string Address)
        {
            this.Address = Address;

            if (this.SelfCheck)
            {
                return;
            }

            CoinCheckResults.AddRange(await Debank.GetAccountsFromAddress(this.Address).GetBalances());
        }

        private double Balance()
        {
            double Total = 0.0;

            foreach (var CoinCheckResult in CoinCheckResults)
            {
                Total += CoinCheckResult.Price;

                if (CoinCheckResult.Tokens.Count > 0)
                {
                    foreach (var Token in CoinCheckResult.Tokens)
                    {
                        Total += Token.Price;
                    }
                }
            }

            return Total;
        }
        public ConsoleReport ConsoleReport(bool IsPrivateKey = false, bool IsAddress = false)
        {
            if (!IsPrivateKey && !IsAddress)
            {
                return new(Balance(), this.CoinCheckResults, this.Mnemonic, this.SelfCheck);
            }
            else if (IsPrivateKey)
            {
                return new(Balance(), this.CoinCheckResults, this.PrivateKey, this.SelfCheck);
            }
            else
            {
                return new(Balance(), this.CoinCheckResults, this.Address, this.SelfCheck);
            }
        }
        public TextReport TextReport(bool IsPrivateKey = false, bool IsAddress = false)
        {
            StringBuilder Report = new();
            IEnumerable<string> Protocols;
            string Footer = string.Empty;


            if (!IsPrivateKey && !IsAddress)
            {
                Footer = $"==================== {Balance():N2} USD ====================> Mnemonic: [{(this.SelfCheck == false ? this.Mnemonic : "self-check")}]\n\n";
            }
            else if (IsPrivateKey)
            {
                Footer = $"==================== {Balance():N2} USD ====================> Private Key: [{(this.SelfCheck == false ? this.PrivateKey : "self-check")}]\n\n";
            }
            else
            {
                Footer = $"==================== {Balance():N2} USD ====================> Address: [{(this.SelfCheck == false ? this.Address : "self-check")}]\n\n";
            }

            Protocols = CoinCheckResults.SelectMany(CoinCheckResult => CoinCheckResult.Tokens).Where(Token => Token.TokenType.Equals(TokenType.PROTOCOL)).Select(Token => Token.Name);

            Report.Append(Footer);
            int NftCount = 0;
            foreach (var CoinCheckResult in CoinCheckResults)
            {
                

                if (!CoinCheckResult.Error)
                {
                    if(CoinCheckResult.CoinType.Equals(CoinType.DEBANK) && (CoinCheckResult.Price > 0.0 || CoinCheckResult.Balance > 0.0 || CoinCheckResult.Tokens.Sum(Token => Token.Balance) > 0 || CoinCheckResult.Tokens.Any(Token => Token.TokenType.Equals(TokenType.PROTOCOL))))
                    {
                        Report.Append($" {CoinCheckResult.CoinType.ToString()}: {CoinCheckResult.Address} - Private Key: {CoinCheckResult.PrivateKey}\n");
                    }
                    else if (CoinCheckResult.Price > 0.0 || CoinCheckResult.Balance > 0.0 || CoinCheckResult.Tokens.Sum(Token => Token.Balance) > 0)
                    {
                        Report.Append($" {CoinCheckResult.CoinType}: {CoinCheckResult.Address} - Balance: {(CoinCheckResult.Price > 0.1 ? CoinCheckResult.Price.ToString("N4") : "< 0.1")} USD ({CoinCheckResult.Balance.ToString("N10")} {CoinCheckResult.CoinType.ToString()}) - Private Key: {CoinCheckResult.PrivateKey}\n");
                    }

                    if (CoinCheckResult.Tokens.Count > 0)
                    {
                        double TokenBalances = 0.0;
                        foreach (var TokenBalance in CoinCheckResult.Tokens)
                        {
                            TokenBalances += TokenBalance.Price;
                        }

                        if (TokenBalances > 0.0 || CoinCheckResult.Tokens.Where(Token => Token.TokenType == TokenType.NFT).Any())
                        {
                            string TokenFooter = $"\n------------------- {CoinCheckResult.CoinType.ToString()} --------------------- Tokens>\n\n";
                            int TokenFooterLenght = TokenFooter.Length;
                            Report.Append(TokenFooter);

                            foreach (var TokenCheckResult in CoinCheckResult.Tokens.AsEnumerable().OrderByDescending(Token => Token.Price))
                            {
                                if (TokenCheckResult.Price > 0.0 || TokenCheckResult.TokenType == TokenType.NFT)
                                {
                                    if (TokenCheckResult.Name.Length > 1)
                                    {
                                        if (TokenCheckResult.TokenType == TokenType.NFT)
                                        {
                                            NftCount++;
                                            Report.Append($" {TokenCheckResult.TokenType} | {TokenCheckResult.Name}: {CoinCheckResult.Address} - Balance: {TokenCheckResult.Balance} {TokenCheckResult.Name}{(TokenCheckResult.Contract.Length > 0 ? " - Contract: " + TokenCheckResult.Contract + "\n" : string.Empty + "\n")}");
                                        }
                                        else
                                        {
                                            if(TokenCheckResult.TokenType == TokenType.PROTOCOL)
                                            {
                                                Report.Append($" {TokenCheckResult.TokenType} | {TokenCheckResult.Name}: {CoinCheckResult.Address} - Balance: {(TokenCheckResult.Price > 0.1 ? TokenCheckResult.Price.ToString("N4") : "< 0.1")} USD\n");
                                            } else
                                            {
                                                Report.Append($" {TokenCheckResult.TokenType} | {TokenCheckResult.Name}: {CoinCheckResult.Address} - Balance: {(TokenCheckResult.Price > 0.1 ? TokenCheckResult.Price.ToString("N4") : "< 0.1")} USD ({TokenCheckResult.Balance.ToString("N10")} {TokenCheckResult.Name}){(TokenCheckResult.Contract.Length > 0 ? " - Contract: " + TokenCheckResult.Contract + "\n" : string.Empty + "\n")}");
                                            }
                                            
                                        }
                                    }

                                }
                            }
                            Report.Append($"\n{string.Concat(Enumerable.Repeat("-", TokenFooterLenght - 1)) + "<"}\n\n");
                        }
                    }
                }
                else
                {
                    //Report.Append($" {CoinCheckResult.CoinType}: {CoinCheckResult.Address} - Error: {CoinCheckResult.ErrorMessage}\n");
                }
            }
            Report.Append("\n" + string.Concat(Enumerable.Repeat("=", Footer.Length - 1)) + "<" + "\n\n\n");

            var Services = CoinCheckResults.Where(CoinCheckResult => CoinCheckResult.Price > 0 || CoinCheckResult.Tokens.Sum(Token => Token.Price) > 0).Select(CoinCheckResult => CoinCheckResult.CoinType).ToList();

            if (CoinCheckResults.Any(CoinCheckResult => CoinCheckResult.CoinType == CoinType.DEBANK))
            {
                return new(Balance(), Report.ToString(), IsPrivateKey == true ? this.PrivateKey : IsAddress ? this.Address : this.Mnemonic, NftCount, Services, CoinCheckResults.Where(CoinCheckResult => CoinCheckResult.CoinType == CoinType.DEBANK).SelectMany(CoinCheckResult => CoinCheckResult.Tokens).Where(Token => Token.TokenType == TokenType.PROTOCOL).Sum(Token => Token.Price), Protocols.ToList());
            } else
            {
                return new(Balance(), Report.ToString(), IsPrivateKey == true ? this.PrivateKey : IsAddress ? this.Address : this.Mnemonic, NftCount, Services, 0, Protocols.ToList());
            }
            
        }
        public JsonReport JsonReport()
        {
            return new(Balance(), this.CoinCheckResults.AsEnumerable().OrderByDescending(Balance => Balance.Price).ToList(), this.Mnemonic);
        }
        public TelegramReport TelegramReport()
        {
            return new(Balance(), this.Mnemonic);
        }
    }
    public static class EngineSelfCheck
    {
        public static async Task<ConsoleReport> SelfCheck(IEnumerable<CoinType> Coins, NetworkSettignsStorage NetworkSettigns)
        {
            Console.Write(" Self-check in progress - ");

            Python Python = new();
            List<Task<CoinCheckResult>> ProcessedTasks = new();
            List<Task<CoinCheckResult>> Tasks = new();
            List<Pair<CoinType, long>> TimeElapsed = new();
            var StopWatch = Stopwatch.StartNew();

            foreach (var Coin in Coins)
            {
                switch (Coin)
                {
                   /* case CoinType.DEBANK:
                        Tasks.Add(new Debank(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;*/
                    case CoinType.ALGO:
                        Tasks.Add(new Algorand(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.ETC:
                        Tasks.Add(new EthereumClassic(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BTC_LEDGER:
                        Tasks.Add(new FructoseCheckerV1.Factory.BitcoinLedger(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BTC_BIP84:
                        Tasks.Add(new FructoseCheckerV1.Factory.BitcoinBip84(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BTC_BIP44:
                        Tasks.Add(new FructoseCheckerV1.Factory.BitcoinBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.CARDANO_SHELLEY:
                        Tasks.Add(new FructoseCheckerV1.Factory.Cardano(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BTC_BIP49:
                        Tasks.Add(new FructoseCheckerV1.Factory.BitcoinBip49(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BTC_BIP86:
                        Tasks.Add(new FructoseCheckerV1.Factory.BitcoinBip86(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BCH_BIP44:
                        Tasks.Add(new BitcoinCashBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BCH_BIP49:
                        Tasks.Add(new BitcoinCashBip49(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.LTC_BIP44:
                        Tasks.Add(new LitecoinBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.LTC_BIP49:
                        Tasks.Add(new LitecoinBip49(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.LTC_BIP84:
                        Tasks.Add(new LitecoinBip84(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.SOL:
                        Tasks.Add(new Solana(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.SOL_PHANTOM:
                        Tasks.Add(new SolanaPhantom(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.ATOM:
                        Tasks.Add(new Cosmos(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.DOT_SUBSTRATE:
                        Tasks.Add(new Polkadot(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.XRP:
                        Tasks.Add(new Ripple(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.XTZ:
                        Tasks.Add(new Tezos(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.DASH_BIP44:
                        Tasks.Add(new DashBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.DASH_BIP49:
                        Tasks.Add(new DashBip49(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.DOGE_BIP44:
                        Tasks.Add(new DogecoinBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.DOGE_BIP49:
                        Tasks.Add(new DogecoinBip49(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.THETA:
                        Tasks.Add(new Theta(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.ZEC_BIP44:
                        Tasks.Add(new ZCashBip44(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.VET:
                        Tasks.Add(new VeChain(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.TRX:
                        Tasks.Add(new Tron(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    case CoinType.BNB:
                        Tasks.Add(new BinanceChain(ref Python, true).ConfigureNetwork(NetworkSettigns).GetSelfCheckBalance());
                        break;
                    default:
                        break;
                }
            }

            int ProcessedTasksCount = 0;
            int SuccessProcessedTasksCount = 0;
            int UnprocessedTasksCount = Tasks.Count;

            using (var Progress = new ConsoleProgress())
            {
                while (Tasks.Any())
                {
                    Progress.Report((double)ProcessedTasksCount / UnprocessedTasksCount);
                    var ProcessedTask = await Task.WhenAny(Tasks);

                    TimeElapsed.Add(new(ProcessedTask.Result.CoinType, StopWatch.ElapsedMilliseconds));

                    if ((await ProcessedTask).Balance > 0.0 && (await ProcessedTask).Price > 0.0)
                    {
                        SuccessProcessedTasksCount++;
                    }
                    else
                    {
                        if ((await ProcessedTask).Error == false && (await ProcessedTask).ErrorMessage == string.Empty)
                        {
                            SuccessProcessedTasksCount++;
                        }
                    }

                    ProcessedTasksCount++;
                    Tasks.Remove(ProcessedTask);
                    ProcessedTasks.Add(ProcessedTask);
                }
            }

            if (SuccessProcessedTasksCount == UnprocessedTasksCount)
            {
                Console.WriteLine($" Self-check passed: {SuccessProcessedTasksCount}/{UnprocessedTasksCount} in {StopWatch.ElapsedMilliseconds / 1000} sec.", Color.GreenYellow);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($" Self-check completed with errors: {SuccessProcessedTasksCount}/{UnprocessedTasksCount} in {StopWatch.ElapsedMilliseconds / 1000} sec.", Color.Red);

                foreach (var Task in ProcessedTasks)
                {
                    if (/*(await Task).Balance <= 0.1 && (await Task).Price <= 0.1 || */(await Task).Error == true)
                    {
                        Console.WriteLine($" [{(await Task).CoinType}] : {(await Task).ErrorMessage}", Color.Red);
                    }
                }
                Console.WriteLine();
            }
            return new(Balance(new(ProcessedTasks.Select(Task => Task.Result))), new(ProcessedTasks.Select(Task => Task.Result)), string.Empty, true);
        }
        private static double Balance(CoinCheckResults CoinCheckResults)
        {
            double Total = 0.0;

            foreach (var CoinCheckResult in CoinCheckResults)
            {
                Total += CoinCheckResult.Price;

                if (CoinCheckResult.Tokens.Count > 1)
                {
                    foreach (var Token in CoinCheckResult.Tokens)
                    {
                        Total += Token.Price;
                    }
                }
            }

            return Total;
        }
    }

    public class CheckerException : Exception
    {
        public CheckerException()
            : base()
        {

        }

        public CheckerException(string Message)
            : base(Message)
        {
        }

        public CheckerException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class CheckerInvalidMnemonicException : CheckerException
    {
        public CheckerInvalidMnemonicException()
            : base($"==================== Invalid ====================> Mnemonic: [null]\n")
        {

        }

        public CheckerInvalidMnemonicException(string Message)
            : base($"==================== Invalid ====================> Mnemonic: [{Message}]\n")
        {
        }
    }
}
