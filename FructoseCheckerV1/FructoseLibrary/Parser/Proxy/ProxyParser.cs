using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Colorful;

using FructoseLib.CLI.Visual.Dynamic;
using FructoseLib.Parser.Proxy.Settings;

using FructoseLibrary.IO;

namespace FructoseLib.IO.Parser
{
    public sealed record ProxyStatus
    {
        public ProxyStatus(ProxyType ProxyType, ProxyVersion ProxyVersion, bool Online, WebProxy Proxy, long Ping, string IP, bool Rotating = false, string Error = "")
        {
            this.ProxyType = ProxyType;
            this.ProxyVersion = ProxyVersion;
            this.Online = Online;
            this.Proxy = Proxy;
            this.Ping = Ping;
            this.IP = IP;
            this.Rotating = Rotating;
            this.Error = Error;
        }

        public ProxyStatus(ProxyType ProxyType, bool Online, WebProxy Proxy, string Error)
        {
            this.ProxyType = ProxyType;
            this.ProxyVersion = ProxyVersion.Unknown;
            this.Online = Online;
            this.Proxy = Proxy;
            this.Ping = long.MaxValue;
            this.IP = string.Empty;
            this.Rotating = false;
            this.Error = Error;
        }

        public ProxyType ProxyType { get; init; }
        public ProxyVersion ProxyVersion { get; init; }
        public bool Online { get; init; }
        public WebProxy Proxy { get; init; }
        public long Ping { get; init; }
        public string IP { get; init; }
        public bool Rotating { get; init; }
        public string Error { get; init; }
    }
    public enum ProxyType { Http, Socks5, Unknown }
    public enum ProxyVersion { IPV4, IPV6, Unknown }
    file enum ProxyFormat { IpPort, IpPortLoginPassword, HttpIpPort, HttpIpPortLoginPassword, SocksIpPort, SocksIpPortLoginPassword, IpPortDogLoginPassword, LoginPasswordDogIpPort }
    file enum ProxySpeedQuality : long { VeryFast = 2000, Fast = 3000, Medium = 4000, Slow = 5000, VerySlow = 7000, Unusable = 10000 }
    file enum ProxySubNetsQuality : long { Excelent = 50, Good = 20, Normal = 10, Bad = 5, VeryBad = 3 }

    file static class ProxyFormatExtensions
    {
        public static (bool Success, Match? Match) Match(this ProxyFormat Type, string Text)
        {
            var Match = Type.GetRegex().Match(Text);

            if (Match.Success)
            {
                return (true, Match);
            }
            else
            {
                return (false, null);
            }
        }
        public static WebProxy Create(this ProxyFormat Value, Match Match)
        {
            switch(Value)
            {
                case ProxyFormat.IpPort:
                    throw new NotSupportedException();
                case ProxyFormat.IpPortLoginPassword:
                    throw new NotSupportedException();
                case ProxyFormat.HttpIpPort:
                    return new()
                    {
                        Address = new Uri($"{Value.GetProxyType().GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                    };
                case ProxyFormat.HttpIpPortLoginPassword:
                    return new()
                    {
                        Address = new Uri($"{Value.GetProxyType().GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                        Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value)
                    };
                case ProxyFormat.SocksIpPort:
                    return new()
                    {
                        Address = new Uri($"{Value.GetProxyType().GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                    };
                case ProxyFormat.SocksIpPortLoginPassword:
                    return new()
                    {
                        Address = new Uri($"{Value.GetProxyType().GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                        Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value)
                    };
                case ProxyFormat.IpPortDogLoginPassword:
                    throw new NotSupportedException();
                case ProxyFormat.LoginPasswordDogIpPort:
                    throw new NotSupportedException();
                default:
                    throw new NotSupportedException();
            }
        }
        public static Regex GetRegex(this ProxyFormat Value) => Value switch
        {
            ProxyFormat.IpPort => new(@"^((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.IpPortLoginPassword => new(@"^((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+):(.+):(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.HttpIpPort => new(@"^http://((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.HttpIpPortLoginPassword => new(@"^http://((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+):(.+):(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.SocksIpPort => new(@"^socks5://((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.SocksIpPortLoginPassword => new(@"^socks5://((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+):(.+):(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.IpPortDogLoginPassword => new(@"^((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+)\@(.+):(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ProxyFormat.LoginPasswordDogIpPort => new(@"^(.+):(.+)\@((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})):(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            _ => throw new NotSupportedException(),
        };
        public static ProxyType GetProxyType(this ProxyFormat Value) => Value switch
        {
            ProxyFormat.IpPort => ProxyType.Unknown,
            ProxyFormat.IpPortLoginPassword => ProxyType.Unknown,
            ProxyFormat.HttpIpPort => ProxyType.Http,
            ProxyFormat.HttpIpPortLoginPassword => ProxyType.Http,
            ProxyFormat.SocksIpPort => ProxyType.Socks5,
            ProxyFormat.SocksIpPortLoginPassword => ProxyType.Socks5,
            ProxyFormat.IpPortDogLoginPassword => ProxyType.Unknown,
            ProxyFormat.LoginPasswordDogIpPort => ProxyType.Unknown,
            _ => throw new NotSupportedException(),
        };
    }

    file static class ProxyTypeExtensions
    {
        public static string GetPrefix(this ProxyType Value) => Value switch
        {
            ProxyType.Http => "http://",
            ProxyType.Socks5 => "socks5://",
            _ => throw new NotSupportedException(),
        };
    }

    file static class ProxySpeedQualityExtensions
    {
        public static ProxySpeedQuality GetProxySpeedQualityType(long Ping)
        {
            return Ping switch
            {
                long Value when Value <= ((long)ProxySpeedQuality.VeryFast) => ProxySpeedQuality.VeryFast,
                long Value when Value <= ((long)ProxySpeedQuality.Fast) => ProxySpeedQuality.Fast,
                long Value when Value <= ((long)ProxySpeedQuality.Medium) => ProxySpeedQuality.Medium,
                long Value when Value <= ((long)ProxySpeedQuality.Slow) => ProxySpeedQuality.Slow,
                long Value when Value <= ((long)ProxySpeedQuality.VerySlow) => ProxySpeedQuality.VerySlow,
                long Value when Value <= ((long)ProxySpeedQuality.Unusable) => ProxySpeedQuality.Unusable,
                _ => ProxySpeedQuality.Unusable,
            };
        }

        public static System.Drawing.Color GetColor(this ProxySpeedQuality Value) => Value switch
        {
            ProxySpeedQuality.VeryFast => System.Drawing.Color.GreenYellow,
            ProxySpeedQuality.Fast => System.Drawing.Color.GreenYellow,
            ProxySpeedQuality.Medium => System.Drawing.Color.Yellow,
            ProxySpeedQuality.Slow => System.Drawing.Color.Orange,
            ProxySpeedQuality.VerySlow => System.Drawing.Color.OrangeRed,
            ProxySpeedQuality.Unusable => System.Drawing.Color.DarkRed,
            _ => throw new NotSupportedException(),
        };

        public static string GetPrefix(this ProxySpeedQuality Value) => Value switch
        {
            ProxySpeedQuality.VeryFast => "Very Fast",
            ProxySpeedQuality.Fast => "Fast",
            ProxySpeedQuality.Medium => "Medium",
            ProxySpeedQuality.Slow => "Slow",
            ProxySpeedQuality.VerySlow => "Very Slow",
            ProxySpeedQuality.Unusable => "Unusable",
            _ => throw new NotSupportedException(),
        };
    }

    file static class ProxySubnetsQualityExtensions
    {
        public static ProxySubNetsQuality GetProxySubnetsQualityType(long Subnets)
        {
            return Subnets switch
            {
                long Value when Value >= ((long)ProxySubNetsQuality.Excelent) => ProxySubNetsQuality.Excelent,
                long Value when Value >= ((long)ProxySubNetsQuality.Good) => ProxySubNetsQuality.Good,
                long Value when Value >= ((long)ProxySubNetsQuality.Normal) => ProxySubNetsQuality.Normal,
                long Value when Value >= ((long)ProxySubNetsQuality.Bad) => ProxySubNetsQuality.Bad,
                long Value when Value >= ((long)ProxySubNetsQuality.VeryBad) => ProxySubNetsQuality.VeryBad,
                _ => ProxySubNetsQuality.VeryBad,
            };
        }

        public static System.Drawing.Color GetColor(this ProxySubNetsQuality Value) => Value switch
        {
            ProxySubNetsQuality.Excelent => System.Drawing.Color.GreenYellow,
            ProxySubNetsQuality.Good => System.Drawing.Color.GreenYellow,
            ProxySubNetsQuality.Normal => System.Drawing.Color.Yellow,
            ProxySubNetsQuality.Bad => System.Drawing.Color.Red,
            ProxySubNetsQuality.VeryBad => System.Drawing.Color.DarkRed,
            _ => throw new NotSupportedException(),
        };

        public static string GetPrefix(this ProxySubNetsQuality Value) => Value switch
        {
            ProxySubNetsQuality.Excelent => "Excelent",
            ProxySubNetsQuality.Good => "Good",
            ProxySubNetsQuality.Normal => "Normal",
            ProxySubNetsQuality.Bad => "Bad",
            ProxySubNetsQuality.VeryBad => "Very Bad",
            _ => throw new NotSupportedException(),
        };
    }

    public static class ProxyParser
    {
        public static async System.Threading.Tasks.Task<IEnumerable<WebProxy>> GetProxy(string? PathOrUrl, ProxyParserSettings Settings, bool Debank)
        {
            ConcurrentQueue<ProxyStatus> ProxyStatuses = new();
            bool Default = false, Debug = false, Url = false;
        ReadFile:
            if (PathOrUrl == null)
            {
                StyleSheet StyleSheet = new StyleSheet(System.Drawing.Color.White);
                StyleSheet.AddStyle("IPV4", System.Drawing.Color.BlueViolet);
                StyleSheet.AddStyle("IPV6", System.Drawing.Color.BlueViolet);
                StyleSheet.AddStyle("FOR DEBANK", System.Drawing.Color.BlueViolet);
                StyleSheet.AddStyle("HTTP", System.Drawing.Color.Yellow);
                StyleSheet.AddStyle("ROTATING", System.Drawing.Color.Yellow);
                StyleSheet.AddStyle("PATH TO FILE", System.Drawing.Color.Magenta);
                StyleSheet.AddStyle("URL", System.Drawing.Color.Magenta);
                StyleSheet.AddStyle("or", System.Drawing.Color.DarkGray);
                StyleSheet.AddStyle(@"\(changes IP for each request\)", System.Drawing.Color.DarkGray);

                Colorful.Console.WriteStyled($" Specify PATH TO FILE or URL to location of your {(Settings.OnlyIpv6Mode ? "IPV6" : "IPV4")}{((Settings.OnlyHttpMode || Settings.OnlyRotatingMode) ? " only" : string.Empty)}{(Settings.OnlyHttpMode ? " HTTP" : string.Empty)}{(Settings.OnlyRotatingMode ? " ROTATING (changes IP for each request)" : string.Empty)} proxy{(Debank ? " FOR DEBANK" : string.Empty)}: ", StyleSheet);
                

                int CursorTop = Colorful.Console.CursorTop;
                int CursorLeft = Colorful.Console.CursorLeft;

                while (true)
                {
                    PathOrUrl = Colorful.Console.ReadLine().Replace("\"", string.Empty);

                    if (PathOrUrl.Length > 0)
                    {
                        break;
                    }
                    else
                    {
                        Colorful.Console.CursorTop = CursorTop;
                        Colorful.Console.CursorLeft = CursorLeft;
                        continue;
                    }
                }

                Default = false;
            }
            else
            {
                Default = true;
            }


            if (PathOrUrl.Contains(" --debug"))
            {
                Debug = true;
                PathOrUrl = PathOrUrl.Replace(" --debug", string.Empty);
            }

            if (System.Uri.TryCreate(PathOrUrl, UriKind.Absolute, out Uri? Uri) && (Uri.Scheme == System.Uri.UriSchemeHttp || Uri.Scheme == System.Uri.UriSchemeHttps))
            {
                Url = true;
            } else
            {
                Url = false;
            }

            string[] Lines = Array.Empty<string>();

            if (Url == true)
            {
                int Retry = 0;
            Load:
                try
                {
                    Lines = (await (await new HttpClient().GetAsync(PathOrUrl)).Content.ReadAsStringAsync()).Split("\n");
                }
                catch
                {
                    if (Retry < Settings.MaxUrlProxyLoadingTries)
                    {
                        Retry++;
                        goto Load;
                    } else
                    {
                        Log.Print($"Error while requesting a{(Default ? " default" : string.Empty)} URL with a proxy, check your internet connection and try again: {PathOrUrl}", LogType.ERROR);
                        PathOrUrl = null;
                        Default = false;
                        goto ReadFile;
                    }
                }
            }
            else
            {
                try
                {
                    if (!System.IO.File.Exists(PathOrUrl))
                    {
                        Log.Print($"Invalid{(Default ? " default" : string.Empty)} file path, please enter correct path to file with proxy. Make sure the path does not contains invalid characters(Non-ASCII symbols), refers to an existing file, and consists entirely of latin characters(Cyrillic may not work on English versions of Windows)", LogType.ERROR);

                        PathOrUrl = null;
                        Default = false;
                        goto ReadFile;
                    }
                    else
                    {
                        Lines = await System.IO.File.ReadAllLinesAsync(PathOrUrl);
                    }
                }
                catch (Exception)
                {
                    Log.Print($"Error while reading{(Default ? " default" : string.Empty)} file with proxy. Please run software as administrator and try again", LogType.ERROR);

                    PathOrUrl = null;
                    Default = false;
                    goto ReadFile;
                }
            }

            using (var ConsoleProgress = new ConcurrentConsoleProgress($"Processing {(Default ? "default" : "input")} {(Url ? "URL" : "file")} with proxy{(Debank ? " for debank" : string.Empty)}", Lines.Length))
            {
                await Parallel.ForEachAsync(Lines, new ParallelOptions { MaxDegreeOfParallelism = Settings.ProxyCheckingThreads }, async (Line, Token) =>
                {
                    foreach (var ProxyFormat in Enum.GetValues(typeof(ProxyFormat)).Cast<ProxyFormat>())
                    {
                        var (Success, Match) = ProxyFormat.Match(Line);

                        if (Success && Match is not null)
                        {
                            if (ProxyFormat.GetProxyType() != ProxyType.Unknown)
                            {
                                var Status = await CheckProxy(ProxyFormat.GetProxyType(), ProxyFormat.Create(Match), Settings.OnlyRotatingMode);

                                ProxyStatuses.Enqueue(Status);
                            }
                            else
                            {
                                WebProxy HttpProxy = new();
                                WebProxy SocksProxy = new();

                                switch (ProxyFormat)
                                {
                                    case ProxyFormat.IpPort:
                                        HttpProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Http.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                        };
                                        SocksProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Socks5.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                        };
                                        break;
                                    case ProxyFormat.IpPortLoginPassword:
                                        HttpProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Http.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value)
                                        };
                                        SocksProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Socks5.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value)
                                        };
                                        break;
                                    case ProxyFormat.IpPortDogLoginPassword:
                                        HttpProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Http.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value),
                                        };
                                        SocksProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Socks5.GetPrefix()}{Match.Groups[1].Value}:{Match.Groups[2].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[3].Value, Match.Groups[4].Value)
                                        };
                                        break;
                                    case ProxyFormat.LoginPasswordDogIpPort:
                                        HttpProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Http.GetPrefix()}{Match.Groups[3].Value}:{Match.Groups[4].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[1].Value, Match.Groups[2].Value)
                                        };

                                        SocksProxy = new()
                                        {
                                            Address = new Uri($"{ProxyType.Socks5.GetPrefix()}{Match.Groups[3].Value}:{Match.Groups[4].Value}"),
                                            Credentials = new NetworkCredential(Match.Groups[1].Value, Match.Groups[2].Value)
                                        };
                                        break;
                                }


                                Task<ProxyStatus> HttpCheckProxyStatusTask = CheckProxy(ProxyType.Http, HttpProxy, Settings.OnlyRotatingMode);
                                Task<ProxyStatus> SocksCheckProxyStatusTask = CheckProxy(ProxyType.Socks5, SocksProxy, Settings.OnlyRotatingMode);

                                await Task.WhenAll(new[] { HttpCheckProxyStatusTask, SocksCheckProxyStatusTask });


                                var SocksStatus = await SocksCheckProxyStatusTask;
                                var HttpStatus = await HttpCheckProxyStatusTask;

                                ProxyStatuses.Enqueue(HttpStatus);
                                ProxyStatuses.Enqueue(SocksStatus);
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    ConsoleProgress.Report();
                });
            }


            if (Debug)
            {
                foreach (var ProxyStatus in ProxyStatuses) {

                    if (ProxyStatus.Online)
                    {
                        Log.Print($"Online proxy {ProxyStatus.IP} | IP Version - {ProxyStatus.ProxyVersion} | URL - {ProxyStatus.Proxy.Address} | Ping - {ProxyStatus.Ping} ms | Rotating - {ProxyStatus.Rotating}", LogType.OK);
                    } else
                    {
                        Log.Print($"Offline proxy {ProxyStatus.Proxy.Address} | Error - {ProxyStatus.Error}", LogType.WARNING);
                    }
                }
                
            }

            ProxyStatuses = new(ProxyStatuses.Where(ProxyStatus => ProxyStatus.Online));


            if (Settings.OnlyIpv6Mode)
            {
                ProxyStatuses = new(ProxyStatuses.Where(ProxyStatus => ProxyStatus.ProxyVersion == ProxyVersion.IPV6));
            } else
            {
                ProxyStatuses = new(ProxyStatuses.Where(ProxyStatus => ProxyStatus.ProxyVersion == ProxyVersion.IPV4));
            }

            if (Settings.OnlyHttpMode)
            {
                ProxyStatuses = new(ProxyStatuses.Where(ProxyStatus => ProxyStatus.ProxyType == ProxyType.Http));
            }

            if (Settings.OnlyRotatingMode)
            {
                ProxyStatuses = new(ProxyStatuses.Where(ProxyStatus => ProxyStatus.Rotating == true));
            }


            if (ProxyStatuses.IsEmpty)
            {
                Log.Print($"Checking is complete,{(Default ? " default" : string.Empty)} {(Url == true ? "URL" : "file")} not contains a online {(Settings.OnlyIpv6Mode ? "IPV6" : "IPV4")}{((Settings.OnlyHttpMode || Settings.OnlyRotatingMode) ? " only" : string.Empty)}{(Settings.OnlyHttpMode ? " HTTP" : string.Empty)}{(Settings.OnlyRotatingMode ? " ROTATING" : string.Empty)} proxy{(Debank ? " FOR DEBANK" : string.Empty)}\n", LogType.WARNING);
                PathOrUrl = null;
                Default = false;
                goto ReadFile;
            }

            var AvgPing = (long)Math.Round(ProxyStatuses.Where(ProxyStatus => ProxyStatus.Online).Average(ProxyStatus => ProxyStatus.Ping), 0);
            var SubnetsCount = GetSubnets(ProxyStatuses.Select(ProxyStatus => ProxyStatus.IP));

            var ProxySpeedQuality = ProxySpeedQualityExtensions.GetProxySpeedQualityType(AvgPing);
            var SubnetsCountQuality = ProxySubnetsQualityExtensions.GetProxySubnetsQualityType(SubnetsCount);

           
            StyleSheet ResultStyleSheet = new StyleSheet(System.Drawing.Color.White);
            ResultStyleSheet.AddStyle("IPV4", System.Drawing.Color.BlueViolet);
            ResultStyleSheet.AddStyle("IPV6", System.Drawing.Color.BlueViolet);
            ResultStyleSheet.AddStyle("FOR DEBANK", System.Drawing.Color.BlueViolet);
            ResultStyleSheet.AddStyle("HTTP", System.Drawing.Color.Yellow);
            ResultStyleSheet.AddStyle("ROTATING", System.Drawing.Color.Yellow);
            ResultStyleSheet.AddStyle(@"\d+ online", System.Drawing.Color.GreenYellow);
            ResultStyleSheet.AddStyle(@"\d+ ms", ProxySpeedQuality.GetColor());
            ResultStyleSheet.AddStyle(ProxySpeedQuality.GetPrefix(), ProxySpeedQuality.GetColor());
            ResultStyleSheet.AddStyle($"{SubnetsCount}\\({SubnetsCountQuality.GetPrefix()}\\)", SubnetsCountQuality.GetColor());

            Colorful.Console.WriteLineStyled($" Checking is complete, found {ProxyStatuses.Where(ProxyStatus => ProxyStatus.Online).Count()} online {(Settings.OnlyIpv6Mode ? "IPV6" : "IPV4")}{((Settings.OnlyHttpMode || Settings.OnlyRotatingMode) ? " only" : string.Empty)}{(Settings.OnlyHttpMode ? " HTTP" : string.Empty)}{(Settings.OnlyRotatingMode ? " ROTATING" : string.Empty)} proxy{(Debank ? " FOR DEBANK" : string.Empty)} | Avg Ping - {AvgPing} ms | Speed - {ProxySpeedQuality.GetPrefix()} | Subnets - {SubnetsCount}({SubnetsCountQuality.GetPrefix()})", ResultStyleSheet);

            if(!Default && Settings.SetDefaultProxyLocationCallback is not null)
            {
                if (ConsoleQuestion.GetYesNoAnswerStyled($"Set this {(Url ? "URL" : "file")} as default proxy location{(Debank ? " for debank" : string.Empty)}?", "private", System.Drawing.Color.BlueViolet))
                {
                    await Settings.SetDefaultProxyLocationCallback(PathOrUrl);
                }
            }

            return ProxyStatuses.Select(ProxyStatus => ProxyStatus.Proxy);
        }

        private static async Task<ProxyStatus> CheckProxy(ProxyType ProxyType, WebProxy Proxy, bool OnlyRotatingMode = false)
        {
            if(!OnlyRotatingMode)
            {
                Stopwatch Stopwatch = new(); Stopwatch.Start();
                try
                {
                    var IP = await GetAsync("https://api64.ipify.org/", Proxy);
                    return new(ProxyType, System.Net.IPAddress.Parse(IP).AddressFamily.Equals(AddressFamily.InterNetwork) ? ProxyVersion.IPV4 : ProxyVersion.IPV6, true, Proxy, Stopwatch.ElapsedMilliseconds, IP);
                }
                catch (Exception Ex)
                {
                    return new(ProxyType, false ,Proxy, Ex.Message);
                }
                finally { Stopwatch.Stop(); Stopwatch.Reset(); }
            } else
            {
                Stopwatch Stopwatch = new(); Stopwatch.Start();
                try
                {
                    var IPFirst = await GetAsync("https://api64.ipify.org/", Proxy);
                    var IPSecond = await GetAsync("https://api64.ipify.org/", Proxy);

                    if (IPFirst.Equals(IPSecond))
                    {
                        return new(ProxyType, System.Net.IPAddress.Parse(IPFirst).AddressFamily.Equals(AddressFamily.InterNetwork) ? ProxyVersion.IPV4 : ProxyVersion.IPV6, true, Proxy, Stopwatch.ElapsedMilliseconds / 2, IPSecond, false);
                    }
                    else
                    {
                        return new(ProxyType, System.Net.IPAddress.Parse(IPSecond).AddressFamily.Equals(AddressFamily.InterNetwork) ? ProxyVersion.IPV4 : ProxyVersion.IPV6, true, Proxy, Stopwatch.ElapsedMilliseconds / 2, IPSecond, true);
                    }
                }
                catch (Exception Ex)
                {
                    return new(ProxyType, false, Proxy, Ex.Message);
                }
                finally { Stopwatch.Stop(); Stopwatch.Reset(); }
            }
        }

        private static async Task<string> GetAsync(string Url, WebProxy Proxy)
        {
            HttpClientHandler Handler = new()
            {
                Proxy = Proxy,
                UseProxy = true,
                MaxConnectionsPerServer = 1
            };

            using HttpClient Client = new(Handler)
            {
                Timeout = TimeSpan.FromMilliseconds(10000),
            };

            try
            {
                HttpResponseMessage Response = await Client.GetAsync(Url);
                Response.EnsureSuccessStatusCode();
                return await Response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Handler.Dispose();
                Client.Dispose();
            }
        }

        private static int GetSubnets(IEnumerable<string> IPs)
        {
            var SubNets = new HashSet<string>();

            foreach (var IP in IPs)
            {
                if (System.Net.IPAddress.TryParse(IP, out var ParsedIP))
                {
                    if (ParsedIP.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        var SubnetMask = ParsedIP.GetAddressBytes();
                        SubNets.Add($"{SubnetMask[0]}.{SubnetMask[1]}.{SubnetMask[2]}.0");
                    } else if (ParsedIP.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                    {
                        var AddressBytes = ParsedIP.GetAddressBytes();
                        var MaskMap = new byte[8];
                        Array.Copy(AddressBytes, MaskMap, 8);
                        SubNets.Add(String.Join(":", MaskMap.Select(Mask => Mask.ToString("X2"))));
                    }
                }
            }

            return SubNets.Count;
        }
    }
}
