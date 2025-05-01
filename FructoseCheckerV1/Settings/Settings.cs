using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;
using FructoseCheckerV1.Utils;

using FructoseLib.Types.Ethereum.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using SharpCompress;

namespace FructoseCheckerV1
{
    public record CoinTypeProperty
    {
        public CoinTypeProperty(string Coin, int Count, bool Check)
        {
            this.Coin = Coin;
            this.Depth = Count;
            this.Check = Check;
        }

        [Newtonsoft.Json.JsonProperty]
        private string Coin { get; set; }
        [Newtonsoft.Json.JsonProperty]
        public int Depth { get; set; }
        [Newtonsoft.Json.JsonProperty]
        public bool Check { get; set; }
        [Newtonsoft.Json.JsonIgnore][Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CoinType CoinType { 
            get
            {
                return (CoinType)Enum.Parse(typeof(CoinType), this.Coin);
            } 
        }
    }

    public struct NetworkSettignsStorage
    {
        public NetworkSettignsStorage(int Retry, bool EnableProxy, bool EnableProxyCheck = true)
        {
            this.RetryCountIfError = Retry;
            this.Proxy = null;
        }

        [JsonProperty]
        public int RetryCountIfError { get; set; }
        [JsonIgnore]
        public List<WebProxy> Proxy { get; private set; }
        public NetworkSettignsStorage AddProxy(IEnumerable<WebProxy> ProxyCollection)
        {
            if(Proxy == null)
            {
                Proxy = new();
            }

            if(ProxyCollection.Any())
            {
                Proxy.AddRange(ProxyCollection);
            }
            
            return this;
        }
    }
    public class SettignsStorage
    {
/*        [JsonProperty]
        public bool CheckingLoop { get; set; }
        [JsonProperty]
        public bool SendTelegramNotifications { get; set; }
        [JsonProperty]
        public string TelegramBotToken { get; set; }
        [JsonProperty]
        public long[] TelegramBotUsers { get; set; }*/
        [JsonProperty]
        public string DefaultProxyLocation { get; set; }
        [JsonProperty]
        public bool SelfCheck { get; set; }
        [JsonProperty]
        public int Threads { get; set; }
        [JsonProperty]
        public bool Antipublic {  get; set; }
        [JsonProperty]
        public decimal MinBalanceForReporting { get; set; }
        [JsonProperty]
        public bool OldLogFormat { get; set; }
        [JsonProperty]
        public bool DebankHideProtocolBalances { get; set; }
        [JsonProperty]
        public NetworkSettignsStorage NetworkSettigns { get; set; }
        [JsonProperty]
        public Dictionary<EthereumUnitType, bool> Search { get; set; }
        [JsonProperty]
        public IList<CoinTypeProperty> CoinProperties { get; set; }
    }
    public abstract class SettingsModelBase
    {
        public SettingsModelBase(string File)
        {
            this.File = File;
        }

        private string File { get; set; }

        protected bool FileExist()
        {
            if (System.IO.File.Exists(File))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected string CreateFile()
        {
            System.IO.File.Create(File).Close();
            return this.File;
        }

        protected void WriteFile(string Json)
        {
            using (StreamWriter IoStream = new(File, false, System.Text.Encoding.UTF8))
            {
                IoStream.Write(Json);
                IoStream.Close();
            };
        }

        protected string ReadFile()
        {
            using (StreamReader IoStream = new(File, true))
            {
                string Json = string.Empty;
                string Line = string.Empty;
                while ((Line = IoStream.ReadLine()) != null)
                {
                    Json += Line;
                }
                IoStream.Close();
                return Json;
            };
        }
    }
    public class Settings : SettingsModelBase
    {
        private static readonly string File = Path.GetDirectoryName(FructoseLib.Utils.Imports.GetExecutablePath()) + @"\Settings.json";
        private static bool OnlyDebank = false;
        private SettignsStorage SettignsStorage { get; set; }
        public Settings(string[] Args)
            : base(File)
        {
            if (Args.Length > 0 && Args[0].Contains("onlydebank"))
            {
                OnlyDebank = true;
            }

            SettignsStorage = new();
            if (!FileExist())
            {
                Default();
            } else
            {
                Read();
            }
        }

        private void Read()
        {
            Read:
            try
            {
                this.SettignsStorage = JsonConvert.DeserializeObject<SettignsStorage>(ReadFile());
            }
            catch(Exception)
            {
                Colorful.Console.WriteLine($" Error while reading Settings.json.", Color.Red);
                Console.ReadKey();
                goto Read;
            }

        }

        private void Default()
        {
            CreateFile();
            this.SettignsStorage.SelfCheck = true;
            this.SettignsStorage.Antipublic = true;
            /*this.SettignsStorage.CheckingLoop = true;
            this.SettignsStorage.SendTelegramNotifications = true;
            this.SettignsStorage.TelegramBotToken = "TELEGRAM_BOT_TOKEN_HERE";
            this.SettignsStorage.TelegramBotUsers = [0];*/
            this.SettignsStorage.DefaultProxyLocation = null;
            this.SettignsStorage.MinBalanceForReporting = 1.0m;
            this.SettignsStorage.Threads = 10;
            this.SettignsStorage.OldLogFormat = false;
            this.SettignsStorage.DebankHideProtocolBalances = false;
            this.SettignsStorage.CoinProperties = new List<CoinTypeProperty>() {
                    new CoinTypeProperty(CoinType.DEBANK.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BTC_BIP84.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BTC_BIP44.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BTC_BIP49.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BTC_BIP86.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BCH_BIP44.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BCH_BIP49.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BTC_LEDGER.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.CARDANO_SHELLEY.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.BNB.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.ETC.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.DOT_SUBSTRATE.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.SOL.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.SOL_PHANTOM.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.TRX.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.ATOM.ToString(), 1, false),
                    new CoinTypeProperty(CoinType.VET.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.LTC_BIP44.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.LTC_BIP49.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.LTC_BIP84.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.XRP.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.ALGO.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.XTZ.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.DASH_BIP44.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.DASH_BIP49.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.DOGE_BIP44.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.DOGE_BIP49.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.THETA.ToString(), 1, true),
                    new CoinTypeProperty(CoinType.ZEC_BIP44.ToString(), 1, true),
            };

            SettignsStorage.Search = new Dictionary<EthereumUnitType, bool>()
            {
                { EthereumUnitType.Mnemonic, true },
                { EthereumUnitType.MnemonicFrench, true },
                { EthereumUnitType.MnemonicSpanish, true },
                { EthereumUnitType.MnemonicChinese, true },
                { EthereumUnitType.MnemonicChineseSimplified, true },
                { EthereumUnitType.MnemonicJapanese, true },
                { EthereumUnitType.MnemonicPortugueseBrazil, true },
                { EthereumUnitType.MnemonicCzech, true },
                { EthereumUnitType.PrivateKey, true },
                { EthereumUnitType.Address, true }
            };

            this.SettignsStorage.NetworkSettigns = new(3, true, true);
            WriteFile(JsonConvert.SerializeObject(this.SettignsStorage, Formatting.Indented));
        }

        public async Task WriteDefaultProxyLocation(string PathOrUrl)
        {
            try
            {
                this.SettignsStorage.DefaultProxyLocation = PathOrUrl;
                await System.IO.File.WriteAllTextAsync(File, JsonConvert.SerializeObject(this.SettignsStorage, Formatting.Indented));
            }
            catch
            {
                Log.Print($"Error while writing default proxy location to <Settings.json>", LogType.ERROR);
                while (true) { Console.ReadKey(); }
            }
        }
        public bool SelfCheck { get { return SettignsStorage.SelfCheck; } }

        /*public bool WriteTextReports { get { return SettignsStorage.WriteTextReports;  } }

        public bool WriteJsonReports { get { return SettignsStorage.WriteJsonReports; } }
        */
        public int Threads { 
            get 
            {
                return SettignsStorage.Threads;
                /*if (NetworkSettings.EnableProxy)
                {
                    return (SettignsStorage.Threads > 20) ? 20 : SettignsStorage.Threads;
                } else
                {
                    return (SettignsStorage.Threads > 100) ? 5 : SettignsStorage.Threads;
                }*/
            } 
        }
        public bool OldLogFormat { get { return SettignsStorage.OldLogFormat; } }

        public bool DebankHideProtocolBalances { get { return SettignsStorage.DebankHideProtocolBalances; } }
        public NetworkSettignsStorage NetworkSettings { get { return SettignsStorage.NetworkSettigns; } }

        //public bool WriteZeroReports { get { return SettignsStorage.WriteZeroReports; } }

        //public string MnemonicFilePath { get { return SettignsStorage.MnemonicFilePath; } }

        //public string ProxyFilePath { get { return SettignsStorage.ProxyFilePath; } }
        public string DefaultProxyLocation { get { return SettignsStorage.DefaultProxyLocation; } }
        public decimal MinBalanceForReporting { get { return SettignsStorage.MinBalanceForReporting; } }
        public IEnumerable<CoinTypeProperty> CoinProperties
        {
            get
            {
                if(OnlyDebank)
                {
                    var CoinTypeProperties = (from Coin in SettignsStorage.CoinProperties select Coin);

                    CoinTypeProperties.ForEach(Coin => { if (Coin.CoinType == CoinType.DEBANK) { Coin.Check = true; } else { Coin.Check = false; } } );
                    return CoinTypeProperties.Where(CoinTypeProperty => CoinTypeProperty.Check == true);
                } else
                {
                    return from Coin in SettignsStorage.CoinProperties
                           where Coin.Check == true
                           select Coin;
                }
            }
        }

        public IEnumerable<CoinType> EnabledCoins { 
            get 
            {
                if (OnlyDebank)
                {
                    var CoinTypeProperties = (from Coin in SettignsStorage.CoinProperties select Coin);

                    CoinTypeProperties.ForEach(Coin => { if (Coin.CoinType == CoinType.DEBANK) { Coin.Check = true; } else { Coin.Check = false; } });
                    return CoinTypeProperties.Where(CoinTypeProperty => CoinTypeProperty.Check == true).Select(CoinTypeProperty => CoinTypeProperty.CoinType);
                }
                else
                {
                    return from Coin in SettignsStorage.CoinProperties
                           where Coin.Check == true
                           select Coin.CoinType;
                }
            } 
        }

        public IEnumerable<CoinType> DisabledCoins
        {
            get
            {
                if (OnlyDebank)
                {
                    var CoinTypeProperties = (from Coin in SettignsStorage.CoinProperties select Coin);

                    CoinTypeProperties.ForEach(Coin => { if (Coin.CoinType == CoinType.DEBANK) { Coin.Check = true; } else { Coin.Check = false; } });
                    return CoinTypeProperties.Where(CoinTypeProperty => CoinTypeProperty.Check == false).Select(CoinTypeProperty => CoinTypeProperty.CoinType);
                }
                else
                {
                    return from Coin in SettignsStorage.CoinProperties
                           where Coin.Check == false
                           select Coin.CoinType;
                }


            }
        }

/*        public bool CheckingLoop { get { return SettignsStorage.CheckingLoop; } }
        public bool SendTelegramNotifications { get { return SettignsStorage.SendTelegramNotifications; } }
        public string TelegramBotToken { get { return SettignsStorage.TelegramBotToken; } }
        public long[] TelegramBotUsers { get { return SettignsStorage.TelegramBotUsers; } }*/

        public bool Antipublic {  get {  return SettignsStorage.Antipublic; } }

        public Dictionary<EthereumUnitType, bool> Search { get { return EnabledCoins.Contains(CoinType.DEBANK) ? SettignsStorage.Search : new(SettignsStorage.Search.ExceptBy(new List<EthereumUnitType>() { EthereumUnitType.PrivateKey, EthereumUnitType.Address }, SearchOption => SearchOption.Key)); } }
    }
}
