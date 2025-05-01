using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FructoseCheckerV1.Factory;
using FructoseCheckerV1.Models;
using FructoseCheckerV1.Utils;

using FructoseLib.CLI.Visual.Dynamic;
using FructoseLib.CLI.Visual.Static;
using FructoseLib.IO.Parser;
using FructoseLib.Types.Ethereum;
using FructoseLib.Types.Ethereum.Enums;

using NBitcoin;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


/*using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;*/

using Color = System.Drawing.Color;
using Console = Colorful.Console;
using File = System.IO.File;

namespace FructoseCheckerV1
{
    class Program
    {
        private static readonly string Version = "0.6.0.0";
        private static Python Python = null;
        private static Settings Settings = null;
        private static ReportController ReportController;
        private static ConcurrentQueue<WebProxy> ProxyCollection = new();
        private static ConcurrentQueue<string> MnemonicCollection = new();
        private static ConcurrentQueue<string> PrivateKeyCollection = new();
        private static ConcurrentQueue<string> AddressCollection = new();
        private static ConcurrentQueue<string> InvalidMnemonicCollection = new();
        private static readonly object ConsoleOutputLocker = new();
        private static int Processed = 0;
        /*private static List<IEthereumUnit> CachedUnits = new();*/


        private static IEnumerable<InputNode> LoopModeInputNodes { get; set; } = new List<InputNode>();
        public static async Task Main(string[] Args)
        {
            Settings = new(Args);

            try
            {
                if (!About())
                {
                    goto Exit;
                }

                ProxyCollection = new(await ProxyParser.GetProxy(Settings.DefaultProxyLocation, new() { OnlyIpv6Mode = false, SetDefaultProxyLocationCallback = Settings.WriteDefaultProxyLocation }, false));
                DebankWrapper.Init(ProxyCollection, Settings.DebankHideProtocolBalances);
            }
            catch (Exception Ex)
            {
                Console.WriteLine($" {Ex.Message}\n\n{Ex.StackTrace}", Color.Red);
                goto Exit;
            }

        GetInput:
            try
            {
                var (InputNodes, Units, InvalidUnits, State) = EthereumParser.GetEthereumInput(Args.Where(Arg => !Arg.Equals("--onlydebank")).ToArray(), new()
                {
                    //LoopMode = Settings.CheckingLoop && CachedUnits.Any(),
                    LoopModeInputNodes = LoopModeInputNodes,
                    Antipublic = Settings.Antipublic,
                    DecompressArchives = false,
                    DeleteDecompressedAfter = false,
                    SearchOptions = Settings.Search.Select(Option => (Option.Key, Option.Value)),
                });

                if (State.Equals(EthereumParserState.OnlyPublicBreakRequested))
                {
                    goto GetInput;
                }

/*                if (Settings.CheckingLoop)
                {
                    Units = Units.ExceptBy(CachedUnits.Select(Unit => Unit.Value), Unit => Unit.Value).ToList();

                    if (!Units.Any())
                    {
                        await Task.Delay(30000);
                        goto GetInput;
                    }

                    LoopModeInputNodes = InputNodes;
                    CachedUnits.AddRange(Units);
                }*/

                ReportController = new(Settings.MinBalanceForReporting, Settings.OldLogFormat/*, Settings.SendTelegramNotifications ,Settings.TelegramBotToken, Settings.TelegramBotUsers*/);
                Processed = 0;


                MnemonicCollection = new(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.Mnemonic).Select(Unit => Unit.Value));
                PrivateKeyCollection = new(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.PrivateKey).Select(Unit => Unit.Value));
                AddressCollection = new(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.Address).Select(Unit => Unit.Value));
                InvalidMnemonicCollection = new(InvalidUnits.Where(InvalidUnit => InvalidUnit.Type.GetCategory() == EthereumUnitTypeCategory.Mnemonic).Select(InvalidUnit => InvalidUnit.Value));

                ReportController.SetMnemonicCount(MnemonicCollection.Count());
                ReportController.SetPrivateKeyCount(PrivateKeyCollection.Count());
                ReportController.SetAddressCount(AddressCollection.Count());
                ReportController.SetInvalidMnemonicCount(InvalidMnemonicCollection.Count());
                ReportController.WriteInvalid(InvalidMnemonicCollection);
                ReportController.WriteValid(MnemonicCollection);
                ReportController.WriteValid(PrivateKeyCollection);
                ReportController.WriteValid(AddressCollection);

                if (Settings.SelfCheck == true)
                {
                    if(MnemonicCollection.Count == 0 && (PrivateKeyCollection.Count > 0 || AddressCollection.Count > 0))
                    {
                        await SelfCheck(false, true);
                    } else
                    {
                        await SelfCheck(false ,false);
                    }
                }

                Console.Write(" Start checking on "); Console.Write(Settings.Threads, Color.BlueViolet); Console.Write(" threads...\n");

                Stopwatch StopWatch = Stopwatch.StartNew();

                using (ConsoleTitleProgress Progress = new ConsoleTitleProgress(MnemonicCollection.Count + PrivateKeyCollection.Count + AddressCollection.Count, true))
                {
                    if (!MnemonicCollection.IsEmpty)
                    {
                        await Parallel.ForEachAsync(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.Mnemonic), new ParallelOptions { MaxDegreeOfParallelism = Settings.Threads }, async (Mnemonic, Token) =>
                        {
                            try
                            {
                                Progress.WaitIfPaused();
                                await Check(Mnemonic.Value);
                                Progress.Report();

                                if (Settings.Antipublic)
                                {
                                    Mnemonic.Type.WriteToAp(Mnemonic.Value);
                                }
                                
                            } catch (Exception ex)
                            {
                                Log.Print(ex.Message + "\n\n\n" + ex.StackTrace, LogType.ERROR);
                            }
                        });
                    }

                    if (!PrivateKeyCollection.IsEmpty)
                    {
                        await Parallel.ForEachAsync(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.PrivateKey), new ParallelOptions { MaxDegreeOfParallelism = Settings.Threads }, async(PrivateKey, Token) =>
                        {
                            try
                            {
                                Progress.WaitIfPaused();
                                await CheckPrivateKey(PrivateKey.Value);
                                Progress.Report();
                                if (Settings.Antipublic)
                                {
                                    PrivateKey.Type.WriteToAp(PrivateKey.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Print(ex.Message + "\n\n\n" + ex.StackTrace, LogType.ERROR);
                            }
                        });
                    }

                    if (!AddressCollection.IsEmpty)
                    {
                        await Parallel.ForEachAsync(Units.Where(Unit => Unit.Type.GetCategory() == EthereumUnitTypeCategory.Address), new ParallelOptions { MaxDegreeOfParallelism = Settings.Threads }, async(Address, Token) =>
                        {
                            try
                            {
                                Progress.WaitIfPaused();
                                await CheckAddress(Address.Value);
                                Progress.Report();

                                if (Settings.Antipublic)
                                {
                                    Address.Type.WriteToAp(Address.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Print(ex.Message + "\n\n\n" + ex.StackTrace, LogType.ERROR);
                            }
                        });
                    }
                }

                await Task.Delay(3000);
                ReportController.RenameReportPath();
                Console.Write($"\n Successull checked {MnemonicCollection.Count} mnemonics, {PrivateKeyCollection.Count} private keys and {AddressCollection.Count} addresses in {StopWatch.ElapsedMilliseconds / 1000} sec.\n\n\n", Color.GreenYellow);
                MnemonicCollection = new();
                PrivateKeyCollection = new();
                InvalidMnemonicCollection = new();
                Processed = 0;
                goto GetInput;
            }
            catch (Exception Ex)
            {
                Console.WriteLine($" {Ex.Message} {Ex}", Color.Red);
            }
        Exit:
            await Task.Delay(900000000);
            Console.ReadKey();
        }
        private static bool About()
        {





            Console.WindowHeight = Convert.ToInt32(Console.LargestWindowHeight / 1.2);
            Console.WindowWidth = Convert.ToInt32(Console.LargestWindowWidth / 1.2);
            ConsoleComponents.RenderAbout(Version, "Fructose Checker", NameStringOffset: 3, VersionStringOffset: 5);
            Colorful.Console.ForegroundColor = Color.White;
            
            Python = new();
            Console.Write($"\n - Python: {Python.Version}" +
                          $"\n - Threads: {Settings.Threads}" +
                          $"\n - Antipublic: {Settings.Antipublic}" +
                          $"\n - Min Report Balance: {Settings.MinBalanceForReporting.ToString().Replace(',', '.')} USD" +
                          $"\n - Default Proxy Location: {(Settings.DefaultProxyLocation ?? "None")}" +
                          $"\n - Self-Check: {Settings.SelfCheck}" +
                          $"\n - Coins: {Settings.EnabledCoins.Count()}/{Settings.EnabledCoins.Count() + Settings.DisabledCoins.Count()}"
                          );

            if (Settings.DisabledCoins.Any())
            {
                Console.WriteLine($"\n - Disabled Coins: {String.Join(", ", Settings.DisabledCoins)}\n");
            }
            else
            {
                Console.WriteLine("\n");
            }

            return true;
        }

        private static async Task Check(string Mnemonic)
        {
                var Engine = new Engine(ref Python, Settings.CoinProperties, Settings.NetworkSettings.AddProxy(ProxyCollection));
                await Engine.Check(Mnemonic);
                lock (ConsoleOutputLocker)
                {
                    Interlocked.Increment(ref Processed);
                    ReportController.PrintConsoleReport(Engine.ConsoleReport(), Processed, MnemonicCollection.Count + PrivateKeyCollection.Count + AddressCollection.Count);
                }
                ReportController.WriteTextReport(Engine.TextReport());
        }
        private static async Task CheckPrivateKey(string PrivateKey)
        {

            var Engine = new Engine(ref Python, Settings.CoinProperties, Settings.NetworkSettings.AddProxy(ProxyCollection));
            await Engine.CheckPrivateKey(PrivateKey);
            lock (ConsoleOutputLocker)
            {
                Interlocked.Increment(ref Processed);
                ReportController.PrintConsoleReport(Engine.ConsoleReport(true, false), Processed, MnemonicCollection.Count + PrivateKeyCollection.Count + AddressCollection.Count, true);
            }

            ReportController.WriteTextReport(Engine.TextReport(true, false));

        }
        private static async Task CheckAddress(string Address)
        {
            var Engine = new Engine(ref Python, Settings.CoinProperties, Settings.NetworkSettings.AddProxy(ProxyCollection));
            await Engine.CheckAddress(Address);
            lock (ConsoleOutputLocker)
            {
                Interlocked.Increment(ref Processed);
                ReportController.PrintConsoleReport(Engine.ConsoleReport(false, true), Processed, MnemonicCollection.Count + PrivateKeyCollection.Count + AddressCollection.Count, false, true);
            }

            ReportController.WriteTextReport(Engine.TextReport(false, true));

        }
        private static async Task<bool> SelfCheck(bool Presentaion = false, bool DebankOnly = false)
        {
            ConsoleReport Result;
            if (!DebankOnly)
            {
                Result = await EngineSelfCheck.SelfCheck(Settings.EnabledCoins, Settings.NetworkSettings.AddProxy(ProxyCollection));
            }
            else
            {
                Result = await EngineSelfCheck.SelfCheck(new CoinType[] { CoinType.DEBANK }, Settings.NetworkSettings.AddProxy(ProxyCollection));
            }
            

            if (Presentaion)
            {
                ReportController.PrintConsoleReport(Result, 1, 1);
            }

            return true;
        }
    }

    public class ReportController
    {
/*        static double CoinCheckResultBalance(CoinCheckResult CoinCheckResult)
        {
            double Total = 0.0d;
            Total += CoinCheckResult.Price;

            if (CoinCheckResult.Tokens.Count > 0)
            {
                foreach (var Token in CoinCheckResult.Tokens)
                {
                    Total += Token.Price;
                }
            }

            return Total;
        }
        static string EscapeMessage(string Text, string Except = "()")
        {
            string Replaces = "-_.=+#<>|'()";

            foreach (var Replace in Replaces)
            {
                if (!Except.Contains(Replace))
                {
                    Text = Text.Replace(Replace.ToString(), $"\\{Replace.ToString()}");
                }
            }

            return Text.Replace("\\|\\|", "||");
        }*/
        public ReportController(decimal MinBalanceForReporting, bool OldLogFormat/*, bool SendTelegramNotifications, string TelegramBotToken, long[] TelegramBotUsers*/)
        {
            this.OldLogFormat = OldLogFormat;
/*            this.SendTelegramNotifications = SendTelegramNotifications;
            this.TelegramBotToken = TelegramBotToken;
            this.TelegramBotUsers = TelegramBotUsers;*/
            this.MnemonicCount = 0;
            this.InvalidMnemonicCount = 0;
            this.PrivateKeyCount = 0;
            this.MinBalanceForReporting = MinBalanceForReporting;
            this.WriteZeroReports = false;
            this.OutputPath = Path.GetDirectoryName(Imports.GetExecutablePath()) + @"\Reports\";
            //Bot = new TelegramBotClient(TelegramBotToken);
        }

        private readonly DateTime DateTime = DateTime.Now;
        //private TelegramBotClient Bot { get; init; }
        private bool OldLogFormat {  get; set; }
/*        public bool SendTelegramNotifications { get; }
        private string TelegramBotToken { get; }
        private long[] TelegramBotUsers { get; }*/
        private decimal MinBalanceForReporting { get; set; }
        private double TotalBalanceUsd { get; set; }
        private bool WriteZeroReports { get; init; }
        private string OutputPath { get; init; }
        private string ReportPath
        {
            get
            {
                string ReportPath = Path.Join(this.OutputPath, $"{this.DateTime:dd.MM.yyyy in HH\\h mm\\m} - [Mnemonics - {MnemonicCount}, Private Keys - {PrivateKeyCount}, Addresses - {AddressCount}, Invalid Mnemonics - {InvalidMnemonicCount}]");
                if (!Directory.Exists(ReportPath))
                {
                    CreateDirectory(ReportPath);
                }

                return ReportPath;
            }
        }
        private string InvalidMnemonicsOutputFile
        {
            get
            {
                string InvalidMnemonicsOutputFile = Path.Join(ReportPath, @"Invalid.txt");

                if (!File.Exists(InvalidMnemonicsOutputFile))
                {
                    CreateFile(InvalidMnemonicsOutputFile);
                }
                return InvalidMnemonicsOutputFile;
            }
        }
        private string ValidOutputFile
        {
            get
            {
                string InvalidMnemonicsOutputFile = Path.Join(ReportPath, @"Valid.txt");

                if (!File.Exists(InvalidMnemonicsOutputFile))
                {
                    CreateFile(InvalidMnemonicsOutputFile);
                }
                return InvalidMnemonicsOutputFile;
            }
        }
        private string NonZeroMnemonicsOutputFile
        {
            get
            {
                string InvalidMnemonicsOutputFile = Path.Join(ReportPath, @"NonZero.txt");

                if (!File.Exists(InvalidMnemonicsOutputFile))
                {
                    CreateFile(InvalidMnemonicsOutputFile);
                }
                return InvalidMnemonicsOutputFile;
            }
        }
        private int MnemonicCount { get; set; }
        private int PrivateKeyCount { get; set; }
        private int AddressCount { get; set; }
        private int InvalidMnemonicCount { get; set; }

        public void RenameReportPath()
        {
            try
            {
                Directory.Move(ReportPath, Path.Join(this.OutputPath, $"{TotalBalanceUsd.ToString("N2", new CultureInfo("en-EN")).Replace(" ", string.Empty).Replace(",", string.Empty)}$ - {this.DateTime:dd.MM.yyyy in HH\\h mm\\m} - [Mnemonics - {MnemonicCount}, Private Keys - {PrivateKeyCount}, Addresses - {AddressCount}, Invalid Mnemonics - {InvalidMnemonicCount}]"));
            } catch {
                Log.Print("It is impossible to rename the folder with the result to display the total balance in its name", LogType.ERROR);
            }
        }

        public void SetMnemonicCount(int MnemonicCount)
        {
            this.MnemonicCount = MnemonicCount;
        }

        public void SetPrivateKeyCount(int PrivateKeyCount)
        {
            this.PrivateKeyCount = PrivateKeyCount;
        }

        public void SetAddressCount(int PrivateKeyCount)
        {
            this.AddressCount = PrivateKeyCount;
        }

        public void SetInvalidMnemonicCount(int InvalidMnemonicCount)
        {
            this.InvalidMnemonicCount = InvalidMnemonicCount;
        }

        public bool PrintConsoleReport(ConsoleReport Report, int Processed, int Unprocessed, bool IsPrivateKey = false, bool IsAddress = false)
        {
            TotalBalanceUsd += Report.Balance;

            string Footer = string.Empty;
            if (!IsPrivateKey && !IsAddress)
            {
                Footer = $"\n\n [{Processed}/{Unprocessed}] ==================== {Report.Balance:N2} USD ====================> Mnemonic: [{(Report.SelfCheck == false ? Report.MnemonicOrPrivateKeyOrAddress : "self-check")}]\n"; Console.WriteLine(Footer, Color.BlueViolet);
            } else if(IsPrivateKey)
            {
                Footer = $"\n\n [{Processed}/{Unprocessed}] ==================== {Report.Balance:N2} USD ====================> Private Key: [{(Report.SelfCheck == false ? Report.MnemonicOrPrivateKeyOrAddress : "self-check")}]\n"; Console.WriteLine(Footer, Color.BlueViolet);
            } else
            {
                Footer = $"\n\n [{Processed}/{Unprocessed}] ==================== {Report.Balance:N2} USD ====================> Address: [{(Report.SelfCheck == false ? Report.MnemonicOrPrivateKeyOrAddress : "self-check")}]\n"; Console.WriteLine(Footer, Color.BlueViolet);
            }

            foreach (var CoinCheckResult in Report.CoinCheckResults)
            {
                if (!CoinCheckResult.Error)
                {
                    
                    if (CoinCheckResult.CoinType.Equals(CoinType.DEBANK) && (CoinCheckResult.Price > 0.0 || CoinCheckResult.Balance > 0.0 || CoinCheckResult.Tokens.Sum(Token => Token.Balance) > 0 || CoinCheckResult.Tokens.Any(Token => Token.TokenType.Equals(TokenType.PROTOCOL))))
                    {
                        Console.Write($" {CoinCheckResult.CoinType.ToString()}", Color.BlueViolet);
                        Console.WriteLine($": {CoinCheckResult.Address} - Private Key: {CoinCheckResult.PrivateKey}");
                    } else if (CoinCheckResult.Price > 0.0 || CoinCheckResult.Balance > 0.0 || CoinCheckResult.Tokens.Sum(Token => Token.Balance) > 0)
                    {
                        Console.Write($" {CoinCheckResult.CoinType.ToString()}", Color.BlueViolet);
                        Console.Write($": {CoinCheckResult.Address} - Balance: ");
                        Console.Write($"{(CoinCheckResult.Price > 0.1 ? CoinCheckResult.Price.ToString("N4") : "< 0.1")} USD ({CoinCheckResult.Balance.ToString("N10")} {(CoinCheckResult.CoinType.ToString())})", CoinCheckResult.Price > 0.1 ? Color.Yellow : Color.White);
                        Console.WriteLine($" - Private Key: {CoinCheckResult.PrivateKey}");
                    }

                    if (CoinCheckResult.Tokens.Count > 0)
                    {
                        double TokensPrice = 0.0;
                        CoinCheckResult.Tokens.ToList().ForEach(Token => TokensPrice += Token.Price);

                        if (TokensPrice > 0.0 || CoinCheckResult.Tokens.AsEnumerable().Where(Token => Token.TokenType == TokenType.NFT).Count() > 0)
                        {
                            string TokenFooter = $"\n------------------- {CoinCheckResult.CoinType.ToString()} --------------------- Tokens>\n";
                            Console.WriteLine(TokenFooter, System.Drawing.Color.LightSlateGray);

                            foreach (var TokenCheckResult in CoinCheckResult.Tokens.AsEnumerable().OrderByDescending(Token => Token.Price))
                            {
                                if (TokenCheckResult.Price > 0.0 || TokenCheckResult.TokenType == TokenType.NFT)
                                {
                                    if (TokenCheckResult.Name.Length > 1)
                                    {
                                        Console.Write($" {TokenCheckResult.TokenType.ToString()} | {TokenCheckResult.Name}", Color.Yellow);

                                        if (TokenCheckResult.TokenType == TokenType.NFT)
                                        {

                                            Console.Write($": {CoinCheckResult.Address} - Balance: ");

                                            Console.Write($"{TokenCheckResult.Balance} {TokenCheckResult.Name}", Color.Yellow);
                                        }
                                        else
                                        {
                                            if(TokenCheckResult.TokenType == TokenType.PROTOCOL)
                                            {
                                                Console.Write($": {CoinCheckResult.Address} - Balance: ");
                                                Console.WriteLine($"{(TokenCheckResult.Price > 0.1 ? TokenCheckResult.Price.ToString("N4") : "< 0.1")} USD", TokenCheckResult.Price > 0.1 ? Color.Yellow : Color.White);
                                            } else
                                            {
                                                Console.Write($": {CoinCheckResult.Address} - Balance: ");
                                                Console.Write($"{(TokenCheckResult.Price > 0.1 ? TokenCheckResult.Price.ToString("N4") : "< 0.1")} USD ({TokenCheckResult.Balance.ToString("N10")} {TokenCheckResult.Name})", TokenCheckResult.Price > 0.1 ? Color.Yellow : Color.White);
                                            }
                                        }

                                        if (TokenCheckResult.TokenType != TokenType.PROTOCOL)
                                        {
                                            Console.WriteLine(TokenCheckResult.Contract.Length > 0 ? $" - Contract: {TokenCheckResult.Contract.Normalize()}" : string.Empty);
                                        }
                                    }
                                }
                            }
                            Console.WriteLine($"\n{string.Concat(Enumerable.Repeat("-", TokenFooter.Length - 1)) + "<"}\n", Color.LightSlateGray);
                        }
                    }

/*                    if (SendTelegramNotifications && (CoinCheckResultBalance(CoinCheckResult) > (double)MinBalanceForReporting))
                    {
                        try
                        {
                            foreach (var User in TelegramBotUsers)
                            {
                                Bot.SendTextMessageAsync(new ChatId(User), EscapeMessage($"🔗 CHAIN: {CoinCheckResult.CoinType.ToString()}\r\n📝 ADDRESS: `{CoinCheckResult.Address}`\r\n💲 Balance: {CoinCheckResultBalance(CoinCheckResult)}$\r\n📨\r\n🔑 PrivateKey: `{CoinCheckResult.PrivateKey}`\r\n📄 Seed phrase: `{Report.MnemonicOrPrivateKeyOrAddress}`\r\n➡️ [Open in browser]({string.Format(CoinCheckResult.CoinType.GetExplorerFormated(), CoinCheckResult.Address)})"), parseMode: ParseMode.MarkdownV2);
                            }
                        }
                        catch { }
                    }*/

                } else
                {
                    //Console.WriteLine($" [{CoinCheckResult.CoinType}] Error: {CoinCheckResult.ErrorMessage}", Color.Red);
                }
            }
            return true;
        }

        public bool WriteTextReport(TextReport Report)
        {
            if ((Report.Balance > 0.0 || Report.CoinCheckResults.Contains("NFT") || this.WriteZeroReports == true) && Report.Balance > (double)this.MinBalanceForReporting)
            {
                string TextReportName = string.Empty;

                if (OldLogFormat)
                {
                    TextReportName = Path.Join(ReportPath, $"{Report.Balance.ToString("N2", new CultureInfo("en-EN")).Replace(" ", string.Empty).Replace(",", string.Empty)}${(Report.BalanceInProtocols > 0.0 ? $" ({Report.BalanceInProtocols.ToString("N2", new CultureInfo("en-EN")).Replace(" ", string.Empty).Replace(",", string.Empty)}$ in EVM protocols)" : string.Empty)} - [{Report.MnemonicOrPrivateKeyOrAddress}].txt");
                } else
                {
                    TextReportName = Path.Join(ReportPath, $"{Report.Balance.ToString("N2", new CultureInfo("en-EN")).Replace(" ", string.Empty).Replace(",", string.Empty)}${(Report.BalanceInProtocols > 0.0 ? $" ({Report.BalanceInProtocols.ToString("N2", new CultureInfo("en-EN")).Replace(" ", string.Empty).Replace(",", string.Empty)}$ in EVM protocols)" : string.Empty)} - [{string.Join(", ", Report.Services.Distinct())}].txt");
                }

                if (!File.Exists(TextReportName))
                {
                    CreateFile(TextReportName);
                }


                WriteFile(TextReportName, Report.CoinCheckResults);
                WriteNonZeroMnemonic(Report.MnemonicOrPrivateKeyOrAddress);

                return true;
            } else
            {
                return false;
            }
        }

        public bool WriteJsonReport(JsonReport Report)
        {
            if (this.WriteZeroReports == false && Report.Balance < 0.1)
            {
                return true;
            }

            string JsonReportName = Path.Join(ReportPath, $"{Report.Balance:N2}$ - [{Report.Mnemonic}].json");

            if (!File.Exists(JsonReportName))
            {
                CreateFile(JsonReportName);
            }

            WriteFile(JsonReportName, JsonConvert.SerializeObject(new { Mnemonic = Report.Mnemonic, Balance = Report.Balance, Coins = Report.CoinCheckResults }, Formatting.Indented));

            return true;
        }

        public bool SendTelegramReport(TelegramReport Report)
        {
            throw new NotImplementedException();
        }

        public bool WriteInvalid(IEnumerable<string> InvalidMnemonicCollection)
        {
            if (InvalidMnemonicCollection.Count() == 0)
            {
                return true;
            }

            if (!File.Exists(this.InvalidMnemonicsOutputFile))
            {
                CreateFile(this.InvalidMnemonicsOutputFile);
            }

            StringBuilder Output = new();
            InvalidMnemonicCollection.ToList().ForEach(InvalidMnemonic => Output.Append($"{InvalidMnemonic}\n"));

            WriteFile(this.InvalidMnemonicsOutputFile, Output.ToString());

            return true;
        }

        public bool WriteValid(IEnumerable<string> ValidMnemonicCollection)
        {
            if (ValidMnemonicCollection.Count() == 0)
            {
                return true;
            }

            if (!File.Exists(this.ValidOutputFile))
            {
                CreateFile(this.ValidOutputFile);
            }

            StringBuilder Output = new();
            ValidMnemonicCollection.ToList().ForEach(InvalidMnemonic => Output.Append($"{InvalidMnemonic}\n"));

            WriteFile(this.ValidOutputFile, Output.ToString());

            return true;
        }

        public bool WriteNonZeroMnemonic(string Mnemonic)
        {
            if (!File.Exists(this.NonZeroMnemonicsOutputFile))
            {
                CreateFile(this.NonZeroMnemonicsOutputFile);
            }

            WriteFile(this.NonZeroMnemonicsOutputFile, Mnemonic + "\n");

            return true;
        }

        private string CreateFile(string File)
        {
            System.IO.File.Create(File).Close();
            return File;
        }

        private string CreateDirectory(string Path)
        {
            System.IO.Directory.CreateDirectory(Path);
            return Path;
        }

        private void WriteFile(string File, string Text)
        {
            System.IO.File.AppendAllText(File, Text);
        }
    }

    public class ProgramException : Exception
    {
        public ProgramException()
            : base(string.Empty)
        {
        }

        public ProgramException(string Message)
            : base(Message)
        {
        }

        public ProgramException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class ProgramMnemonicNotFoundException : Exception
    {
        public ProgramMnemonicNotFoundException()
            : base($" File not contains a valid mnemonic phrases.")
        {

        }

        public ProgramMnemonicNotFoundException(string File)
             : base($" File: {File} not contains a valid mnemonic phrases.")
        {
        }

        public ProgramMnemonicNotFoundException(string File, Exception Inner)
            : base($" File: {File} not contains a valid mnemonic phrases.")
        {
        }
    }
    public class ProgramGetResponceException : WalletCheckerException
    {
        public ProgramGetResponceException()
            : base($"Unable to get responce.")
        {
        }

        public ProgramGetResponceException(string Url, string Reason)
            : base($"Unable to get responce from: {Url} - {Reason}.")
        {
        }

        public ProgramGetResponceException(string Url, Exception Inner)
            : base($"Unable to get responce from: {Url}.")
        {
        }
    }
    public class ProgramJsonException : WalletCheckerException
    {
        public ProgramJsonException()
            : base("Unable to parse json object.")
        {

        }

        public ProgramJsonException(string Url, string Reason)
            : base($"Unable to parse json object from: {Url} - {Reason}.")
        {
        }

        public ProgramJsonException(string Url, Exception Inner)
            : base($"Unable to parse json object from: {Url}.")
        {
        }
    }
    public class ProgramProxyNotFoundException : Exception
    {
        public ProgramProxyNotFoundException()
            : base("No proxy found in file.")
        {

        }

        public ProgramProxyNotFoundException(string File)
            : base($"No proxy found in file: {File}.")
        {
        }

        public ProgramProxyNotFoundException(string File, Exception Inner)
            : base($"No proxy found in file: {File}.")
        {
        }
    }
    public class ProgramProxyXpathException : Exception
    {
        public ProgramProxyXpathException()
            : base("Unable to grab proxy.")
        {

        }

        public ProgramProxyXpathException(string Url, string Reason)
            : base($"Unable to grab proxy from: {Url} - {Reason}.")
        {
        }

        public ProgramProxyXpathException(string Url, Exception Inner)
            : base($"Unable to grab proxy: {Url}.")
        {
        }
    }
}
