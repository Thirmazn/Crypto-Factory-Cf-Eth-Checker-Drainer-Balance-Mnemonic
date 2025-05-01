using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Colorful;

using FructoseCheckerV1.Utils;

using FructoseLib.CLI.Visual.Dynamic;
using FructoseLib.Extensions;
using FructoseLib.Types.Ethereum;
using FructoseLib.Types.Ethereum.Enums;

using NBitcoin;

using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

using Console = Colorful.Console;

namespace FructoseLib.IO.Parser
{
    public enum EthereumParserState { Default, OnlyPrivate, OnlyPublic, OnlyPublicBreakRequested }
    internal static class EthereumParser
    {
        private static readonly string[] ForbiddenPathParts = new string[]
        {
            //"Passwords.txt",
            //"Password.txt",
            "ProcessList.txt",
            "System.txt",
            "WebCredential.txt",
            "InstalledBrowsers.txt",
            "UserInformation.txt",
            "DomainDetects.txt",
            "InstalledSoftware.txt",
            "WindowsCredential.txt",
            "CreditCards.txt",
            "Information.txt",
            //"ImportantAutofills.txt",
            "System Info.txt",
            "Domain Detect.txt",
            "History.txt",
            "DiscordTokens.txt",
            "System_Info.txt",
            "Cookies.txt",
            "Cookie_List.txt",
            "Ip.txt",
            "Chrome_Default.txt",
            "Opera.txt",
            "Opera_.txt",
            "OperaGX_.txt",
            "Brave_Default.txt",
            "Edge_Default.txt",
            "CC.txt",
            "Default Network.txt",
            "AllCookies",
            //"AllPasswords",
            "AllHistory",
            "Paranoid",
            "All_CC",
            "DeviceSearchCache_AppCache",
            @"\Cookies",
            @"\_Cookies",
            @"\Cookie",
            @"\History",
            @"\Discord",
            @"\Logins",
            @"\Credits",
            @"\CreditCards",
            //@"\Autofill",
            @"Google Chrome_Profile",
            @"Google Chrome Profile",
            //@"\Autofills",
            @"\CC",
            @"\Discord\Tokens.txt",
            @"\FTP\Credentials.txt",
            $"{System.IO.Path.Combine(System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Windows")}"
        };

        private static readonly string[] FileExtensions = new string[]
        {
            ".txt",
            ".json",
            ".xml",
            ".html",
            ".csv",
            //".log",
            ".tmp",
            ".cfg",
            ".config",
        };

        public static readonly string[] ArchiveFileExtensions = new string[]
        {
            ".rar",
            ".zip",
            ".7z",
        };

        public static (IEnumerable<InputNode> Inputs, IEnumerable<IEthereumUnit> Units, IEnumerable<(EthereumUnitType Type, string Value)> InvalidUnits, EthereumParserState State) GetEthereumInput(string[] Args)
        {
            return GetEthereumInput(Args, new());
        }
        public static (IEnumerable<InputNode> Inputs, IEnumerable<IEthereumUnit> Units, IEnumerable<(EthereumUnitType Type, string Value)> InvalidUnits, EthereumParserState State) GetEthereumInput(string[] Args, EthereumParserSettings Settings)
        {
            if(Settings.LoopMode == true)
            {
                return GetEthereumInputLoop(Args, Settings);
            }

            ConcurrentBag<IEthereumUnit> Units = new();
            ConcurrentBag<(EthereumUnitType Type, string Value)> InvalidUnits = new();
            IEnumerable<InputNode> Inputs = new List<InputNode>();

            if (!Settings.SearchOptions.Where(Option => Option.Enabled).Any())
            {
                Log.Print("For the checker work, at least one search option must be enabled in the Settings.json file", LogType.WARNING);
                while (true) { Console.ReadKey(); }
            }

            List<EthereumSearchTypeStorage> EthereumSearchTypeStorages = new(Settings.SearchOptions.Where(Option => Option.Enabled).Select(Option => new EthereumSearchTypeStorage(Option.UnitType)));

            if (Settings.Antipublic)
            {
                StyleSheet StyleSheet = new StyleSheet(System.Drawing.Color.White);
                StyleSheet.AddStyle(@"\d+", System.Drawing.Color.Orange);
                StyleSheet.AddStyle(@"Antipublic:", System.Drawing.Color.Orange);

                var AntipublicStorages = EthereumSearchTypeStorages
                        .Select(EthereumSearchTypeStorage => (EthereumSearchTypeStorage.Type, Count: EthereumSearchTypeStorage.Type.ReadAp().LongLength))
                        .Where(AntipublicStorage => AntipublicStorage.Count > 0);


                if (AntipublicStorages.Any())
                {
                    Console.WriteLineStyled($"\n Antipublic: {string.Join(" | ", AntipublicStorages.Select(AntipublicStorage => $"{AntipublicStorage.Type} - {AntipublicStorage.Item2}"))}", StyleSheet);
                }
            }

        GetInput:
            Inputs = GetInputs(Args, EthereumSearchTypeStorages, Settings.InExtensionsOptions, Settings.DecompressArchives);


            #region Parsing
            int ProcessedFiles = 0;
            int TotalFiles = Inputs.Sum(Input => Input.Files.Count);

            //Parsing
            using (ConsoleTitleProgress ConsoleTitleProgress = new(TotalFiles, true))
            {
                foreach (var Input in Inputs)
                {
                    var FilesGroups = Input.Files.GroupBy(File =>
                    {
                        if (File.Size < 512000)
                            return 1;
                        else if (File.Size < 5242880)
                            return 2;
                        else
                            return 3;
                    });


                    foreach (var FileGroup in FilesGroups)
                    {
                        int FileGroupKey = FileGroup.Key;

                        //Parsing files content
                        if (FileGroupKey < 3)
                        {
                            Parallel.ForEach(FileGroup, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (FileNode) =>
                            {
                                string[] Lines = Array.Empty<string>();

                                try
                                {
                                    Lines = System.IO.File.ReadAllLines(FileNode.Path);
                                }
                                catch (Exception)
                                {
                                    return;
                                }

                                using (var ConsoleProgress = new ConcurrentConsoleProgress($"[{Interlocked.Increment(ref ProcessedFiles)}/{TotalFiles}] Processing: {FileNode.Path}", Lines.Length))
                                {
                                    foreach (var (Line, Index) in Lines.WithIndex())
                                    {
                                        ConsoleTitleProgress.WaitIfPaused();

                                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                        {
                                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                            foreach (var (Success, Value) in Matches)
                                            {
                                                if (Success == true)
                                                {
                                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                    {
                                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, Index + 1)));
                                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                    else
                                                    {
                                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                }
                                            }
                                        }

                                        ConsoleProgress.Report();
                                    }
                                    /*                            Parallel.ForEach(Lines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, Line =>
                                                                {
                                                                    ConsoleTitleProgress.WaitIfPaused();

                                                                    foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                                                    {
                                                                        IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                                                        foreach (var (Success, Value) in Matches)
                                                                        {
                                                                            if (Success == true)
                                                                            {
                                                                                if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                                                {
                                                                                    Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, FileNode.Hash));
                                                                                    FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                                                }
                                                                                else
                                                                                {
                                                                                    InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                                                    FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    ConsoleProgress.Report();
                                                                });*/
                                }
                                ConsoleTitleProgress.Report();
                            });
                        }
                        else
                        {
                            foreach (var FileNode in FileGroup)
                            {
                                string[] Lines = Array.Empty<string>();

                                try
                                {
                                    Lines = System.IO.File.ReadAllLines(FileNode.Path);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }

                                using (var ConsoleProgress = new ConcurrentConsoleProgress($"[{Interlocked.Increment(ref ProcessedFiles)}/{TotalFiles}] Processing: {FileNode.Path}", Lines.Length))
                                {
                                    Parallel.ForEach(Lines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (Line, State, Index) =>
                                    {
                                        ConsoleTitleProgress.WaitIfPaused();

                                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                        {
                                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                            foreach (var (Success, Value) in Matches)
                                            {
                                                if (Success == true)
                                                {
                                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                    {
                                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, Index + 1)));
                                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                    else
                                                    {
                                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                }
                                            }
                                        }

                                        ConsoleProgress.Report();
                                    });
                                }

                                ConsoleTitleProgress.Report();
                            }
                        }
                    }

                    //Parsing file names
                    foreach (var FileNode in Input.Files)
                    {
                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                        {
                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(FileNode.Path);

                            foreach (var (Success, Value) in Matches)
                            {
                                if (Success == true)
                                {
                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                    {
                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, 0)));
                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                    }
                                    else
                                    {
                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                    }
                                }
                            }
                        }
                    }


                    if (Settings.DeleteDecompressedAfter)
                    {
                        Parallel.ForEach(Input.Decompressed, (DecompressedArchiveDirectory) =>
                        {
                            if (Directory.Exists(DecompressedArchiveDirectory))
                            {
                                Directory.Delete(DecompressedArchiveDirectory, true);
                            }
                        });

                        /*foreach (var DecompressedArchiveDirectory in Input.Decompressed)
                        {
                            if (Directory.Exists(DecompressedArchiveDirectory))
                            {
                                Directory.Delete(DecompressedArchiveDirectory, true);
                            }
                        }*/
                    }
                }
            }


            //Distinct all by interface value property and
            List<IEthereumUnit> UniquePrivateUnits = new();
            List<IEthereumUnit> UniqueUnits = new(Units.DistinctBy(Unit => Unit.Value));

            //Prepare results
            foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
            {
                var SelectedUnits = Units.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type);
                var UniqueSelectedUnits = SelectedUnits.DistinctBy(Unit => Unit.Value);
                var UniqueSelectedInvalidUnits = InvalidUnits.Where(InvalidUnit => InvalidUnit.Type == EthereumSearchTypeStorage.Type).DistinctBy(InvalidUnit => InvalidUnit.Value);
                EthereumSearchTypeStorage.SetValid(UniqueSelectedUnits.Count());
                EthereumSearchTypeStorage.SetInvalid(UniqueSelectedInvalidUnits.Count());
                EthereumSearchTypeStorage.SetDuplicates(SelectedUnits.Count() - EthereumSearchTypeStorage.Valid);
                if (Settings.Antipublic)
                {

                    List<IEthereumUnit> UniqueSelectedPublicUnits = new(UniqueSelectedUnits.IntersectBy(EthereumSearchTypeStorage.Type.ReadAp(), Unit => Unit.Value));
                    List<IEthereumUnit> UniqueSelectedPrivateUnits = new(UniqueSelectedUnits.ExceptBy(UniqueSelectedPublicUnits.Select(Unit => Unit.Value), Unit => Unit.Value));
                    EthereumSearchTypeStorage.SetPublic(UniqueSelectedPublicUnits.Count());
                    EthereumSearchTypeStorage.SetPrivate(EthereumSearchTypeStorage.Valid - EthereumSearchTypeStorage.Public);
                    if (UniqueSelectedPrivateUnits.Any())
                    {
                        UniquePrivateUnits.AddRange(UniqueSelectedPrivateUnits);
                    }
                }
            }

            #endregion

            if (Settings.Antipublic)
            {
                #region Results

                if (EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) == 0)
                {
                    Log.Print($"The file does not contains valid: {string.Join(" | ", EthereumSearchTypeStorages.Select(Option => Option.Type.GetTag()))}", LogType.WARNING);

                    if (Args.Length > 0)
                    {
                        Args = Array.Empty<string>();
                    }

                    goto GetInput;
                }
                else
                {
                    Formatter[] ProcessingCompleteFormat = new Formatter[]
                    {
                        new Formatter("valid", EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                        new Formatter("invalid", EthereumSearchTypeStorages.Sum(Unit => Unit.Invalid) > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                        new Formatter("duplicates", EthereumSearchTypeStorages.Sum(Unit => Unit.Duplicates) > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                        new Formatter("public", EthereumSearchTypeStorages.Sum(Unit => Unit.Public) > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                        new Formatter("private", EthereumSearchTypeStorages.Sum(Unit => Unit.Private) > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray),
                    };


                    Console.WriteFormatted("\n Processing is complete, found {0} / {1} / {2} / {3} / {4}: ", System.Drawing.Color.White, ProcessingCompleteFormat);

                    foreach (var (EthereumUnit, Index) in EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).WithIndex())
                    {
                        if (EthereumUnit.Total > 0 || EthereumUnit.Invalid > 0)
                        {
                            if (EthereumUnit.Type == EthereumUnitType.Address)
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter("NaN", System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Public, EthereumUnit.Public > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Private, EthereumUnit.Private > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray)
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                            else
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Invalid, EthereumUnit.Invalid > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Public, EthereumUnit.Public > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Private, EthereumUnit.Private > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray)
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region Results

                if (EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) == 0)
                {
                    Log.Print($"The file does not contains valid: {string.Join(" | ", EthereumSearchTypeStorages.Select(Option => Option.Type.GetTag()))}", LogType.WARNING);

                    if (Args.Length > 0)
                    {
                        Args = Array.Empty<string>();
                    }

                    goto GetInput;
                }
                else
                {
                    Formatter[] ProcessingCompleteFormat = new Formatter[]
                    {
                        new Formatter("valid", EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                        new Formatter("invalid", EthereumSearchTypeStorages.Sum(Unit => Unit.Invalid) > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                        new Formatter("duplicates", EthereumSearchTypeStorages.Sum(Unit => Unit.Duplicates) > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                    };

                    Console.WriteFormatted("\n Processing is complete, found {0} / {1} / {2}: ", System.Drawing.Color.White, ProcessingCompleteFormat);

                    foreach (var (EthereumUnit, Index) in EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).WithIndex())
                    {
                        if (EthereumUnit.Total > 0 || EthereumUnit.Invalid > 0)
                        {
                            if (EthereumUnit.Type == EthereumUnitType.Address)
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter("NaN", System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                            else
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Invalid, EthereumUnit.Invalid > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                        }
                    }
                }
                #endregion
            }

/*            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
                if (ConsoleQuestion.GetYesNoAnswerStyled("Add private entries to antipublic?", "private", System.Drawing.Color.BlueViolet))
                {
                    foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                    {
                        if (UniquePrivateUnits.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type).Any())
                        {
                            EthereumSearchTypeStorage.Type.WriteToAp(UniquePrivateUnits.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type).Select(Unit => Unit.Value).ToArray());
                        }
                    }
                }
            }*/

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Public) > 0 && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
                if (ConsoleQuestion.GetYesNoAnswerStyled("Remove public entries?", "public", System.Drawing.Color.Orange))
                {
                    return (Inputs, UniquePrivateUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPrivate);
                }
                else
                {
                    return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.Default);
                }
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) == 0)
            {
                if (ConsoleQuestion.GetYesNoAnswerStyled("All entries are already in the public, do you want to continue?", "public", System.Drawing.Color.Orange))
                {
                    return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPublic);
                }
                else
                {
                    return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPublicBreakRequested);
                }
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Public) == 0 && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
                return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPrivate);
            }
            else
            {
                return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.Default);
            }

        }
        public static (IEnumerable<InputNode> Inputs, IEnumerable<IEthereumUnit> Units, IEnumerable<(EthereumUnitType Type, string Value)> InvalidUnits, EthereumParserState State) GetEthereumInputLoop(string[] Args, EthereumParserSettings Settings)
        {
            ConcurrentBag<IEthereumUnit> Units = new();
            ConcurrentBag<(EthereumUnitType Type, string Value)> InvalidUnits = new();
            IEnumerable<InputNode> Inputs = Settings.LoopModeInputNodes;

            if (!Settings.SearchOptions.Where(Option => Option.Enabled).Any())
            {
                Log.Print("For the checker work, at least one search option must be enabled in the Settings.json file", LogType.WARNING);
                while (true) { Console.ReadKey(); }
            }

            List<EthereumSearchTypeStorage> EthereumSearchTypeStorages = new(Settings.SearchOptions.Where(Option => Option.Enabled).Select(Option => new EthereumSearchTypeStorage(Option.UnitType)));

            if (Settings.Antipublic)
            {
                StyleSheet StyleSheet = new StyleSheet(System.Drawing.Color.White);
                StyleSheet.AddStyle(@"\d+", System.Drawing.Color.Orange);
                StyleSheet.AddStyle(@"Antipublic:", System.Drawing.Color.Orange);

                var AntipublicStorages = EthereumSearchTypeStorages
                        .Select(EthereumSearchTypeStorage => (EthereumSearchTypeStorage.Type, Count: EthereumSearchTypeStorage.Type.ReadAp().LongLength))
                        .Where(AntipublicStorage => AntipublicStorage.Count > 0);


                if (AntipublicStorages.Any())
                {
                    Console.WriteLineStyled($"\n Antipublic: {string.Join(" | ", AntipublicStorages.Select(AntipublicStorage => $"{AntipublicStorage.Type} - {AntipublicStorage.Item2}"))}", StyleSheet);
                }
            }

        GetInput:
            //Inputs = GetInputs(Args, EthereumSearchTypeStorages, Settings.InExtensionsOptions, Settings.DecompressArchives);


            #region Parsing
            int ProcessedFiles = 0;
            int TotalFiles = Inputs.Sum(Input => Input.Files.Count);

            //Parsing
            using (ConsoleTitleProgress ConsoleTitleProgress = new(TotalFiles, true))
            {
                foreach (var Input in Inputs)
                {
                    var FilesGroups = Input.Files.GroupBy(File =>
                    {
                        if (File.Size < 512000)
                            return 1;
                        else if (File.Size < 5242880)
                            return 2;
                        else
                            return 3;
                    });


                    foreach (var FileGroup in FilesGroups)
                    {
                        int FileGroupKey = FileGroup.Key;

                        //Parsing files content
                        if (FileGroupKey < 3)
                        {
                            Parallel.ForEach(FileGroup, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (FileNode) =>
                            {
                                string[] Lines = Array.Empty<string>();

                                try
                                {
                                    Lines = System.IO.File.ReadAllLines(FileNode.Path);
                                }
                                catch (Exception)
                                {
                                    return;
                                }

                                using (var ConsoleProgress = new ConcurrentConsoleProgress($"[{Interlocked.Increment(ref ProcessedFiles)}/{TotalFiles}] Processing: {FileNode.Path}", Lines.Length))
                                {
                                    foreach (var (Line, Index) in Lines.WithIndex())
                                    {
                                        ConsoleTitleProgress.WaitIfPaused();

                                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                        {
                                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                            foreach (var (Success, Value) in Matches)
                                            {
                                                if (Success == true)
                                                {
                                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                    {
                                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, Index + 1)));
                                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                    else
                                                    {
                                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                }
                                            }
                                        }

                                        ConsoleProgress.Report();
                                    }
                                    /*                            Parallel.ForEach(Lines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, Line =>
                                                                {
                                                                    ConsoleTitleProgress.WaitIfPaused();

                                                                    foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                                                    {
                                                                        IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                                                        foreach (var (Success, Value) in Matches)
                                                                        {
                                                                            if (Success == true)
                                                                            {
                                                                                if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                                                {
                                                                                    Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, FileNode.Hash));
                                                                                    FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                                                }
                                                                                else
                                                                                {
                                                                                    InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                                                    FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    ConsoleProgress.Report();
                                                                });*/
                                }
                                ConsoleTitleProgress.Report();
                            });
                        }
                        else
                        {
                            foreach (var FileNode in FileGroup)
                            {
                                string[] Lines = Array.Empty<string>();

                                try
                                {
                                    Lines = System.IO.File.ReadAllLines(FileNode.Path);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }

                                using (var ConsoleProgress = new ConcurrentConsoleProgress($"[{Interlocked.Increment(ref ProcessedFiles)}/{TotalFiles}] Processing: {FileNode.Path}", Lines.Length))
                                {
                                    Parallel.ForEach(Lines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (Line, State, Index) =>
                                    {
                                        ConsoleTitleProgress.WaitIfPaused();

                                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                                        {
                                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(Line);

                                            foreach (var (Success, Value) in Matches)
                                            {
                                                if (Success == true)
                                                {
                                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                                    {
                                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, Index + 1)));
                                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                    else
                                                    {
                                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                                    }
                                                }
                                            }
                                        }

                                        ConsoleProgress.Report();
                                    });
                                }

                                ConsoleTitleProgress.Report();
                            }
                        }
                    }

                    //Parsing file names
                    foreach (var FileNode in Input.Files)
                    {
                        foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                        {
                            IEnumerable<(bool Success, string Value)> Matches = EthereumSearchTypeStorage.Type.Matches(FileNode.Path);

                            foreach (var (Success, Value) in Matches)
                            {
                                if (Success == true)
                                {
                                    if (EthereumSearchTypeStorage.Type.IsValid(Value))
                                    {
                                        Units.Add(EthereumSearchTypeStorage.Type.Create(Value, Settings.DerivationPathsOptions, (FileNode.Hash, 0)));
                                        FileNode.Finded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                    }
                                    else
                                    {
                                        InvalidUnits.Add(new(EthereumSearchTypeStorage.Type, Value));
                                        FileNode.InvalidFinded.AddOrUpdate(EthereumSearchTypeStorage.Type.GetCategory(), 1, (Key, OldValue) => OldValue + 1);
                                    }
                                }
                            }
                        }
                    }


                    if (Settings.DeleteDecompressedAfter)
                    {
                        Parallel.ForEach(Input.Decompressed, (DecompressedArchiveDirectory) =>
                        {
                            if (Directory.Exists(DecompressedArchiveDirectory))
                            {
                                Directory.Delete(DecompressedArchiveDirectory, true);
                            }
                        });

                        /*foreach (var DecompressedArchiveDirectory in Input.Decompressed)
                        {
                            if (Directory.Exists(DecompressedArchiveDirectory))
                            {
                                Directory.Delete(DecompressedArchiveDirectory, true);
                            }
                        }*/
                    }
                }
            }


            //Distinct all by interface value property and
            List<IEthereumUnit> UniquePrivateUnits = new();
            List<IEthereumUnit> UniqueUnits = new(Units.DistinctBy(Unit => Unit.Value));

            //Prepare results
            foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
            {
                var SelectedUnits = Units.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type);
                var UniqueSelectedUnits = SelectedUnits.DistinctBy(Unit => Unit.Value);
                var UniqueSelectedInvalidUnits = InvalidUnits.Where(InvalidUnit => InvalidUnit.Type == EthereumSearchTypeStorage.Type).DistinctBy(InvalidUnit => InvalidUnit.Value);
                EthereumSearchTypeStorage.SetValid(UniqueSelectedUnits.Count());
                EthereumSearchTypeStorage.SetInvalid(UniqueSelectedInvalidUnits.Count());
                EthereumSearchTypeStorage.SetDuplicates(SelectedUnits.Count() - EthereumSearchTypeStorage.Valid);
                if (Settings.Antipublic)
                {

                    List<IEthereumUnit> UniqueSelectedPublicUnits = new(UniqueSelectedUnits.IntersectBy(EthereumSearchTypeStorage.Type.ReadAp(), Unit => Unit.Value));
                    List<IEthereumUnit> UniqueSelectedPrivateUnits = new(UniqueSelectedUnits.ExceptBy(UniqueSelectedPublicUnits.Select(Unit => Unit.Value), Unit => Unit.Value));
                    EthereumSearchTypeStorage.SetPublic(UniqueSelectedPublicUnits.Count());
                    EthereumSearchTypeStorage.SetPrivate(EthereumSearchTypeStorage.Valid - EthereumSearchTypeStorage.Public);
                    if (UniqueSelectedPrivateUnits.Any())
                    {
                        UniquePrivateUnits.AddRange(UniqueSelectedPrivateUnits);
                    }
                }
            }

            #endregion

            if (Settings.Antipublic)
            {
                #region Results

                if (EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) == 0)
                {
                    Log.Print($"The file does not contains valid: {string.Join(" | ", EthereumSearchTypeStorages.Select(Option => Option.Type.GetTag()))}", LogType.WARNING);

                    if (Args.Length > 0)
                    {
                        Args = Array.Empty<string>();
                    }

                    goto GetInput;
                }
                else
                {
                    Formatter[] ProcessingCompleteFormat = new Formatter[]
                    {
                        new Formatter("valid", EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                        new Formatter("invalid", EthereumSearchTypeStorages.Sum(Unit => Unit.Invalid) > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                        new Formatter("duplicates", EthereumSearchTypeStorages.Sum(Unit => Unit.Duplicates) > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                        new Formatter("public", EthereumSearchTypeStorages.Sum(Unit => Unit.Public) > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                        new Formatter("private", EthereumSearchTypeStorages.Sum(Unit => Unit.Private) > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray),
                    };


                    Console.WriteFormatted("\n Processing is complete, found {0} / {1} / {2} / {3} / {4}: ", System.Drawing.Color.White, ProcessingCompleteFormat);

                    foreach (var (EthereumUnit, Index) in EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).WithIndex())
                    {
                        if (EthereumUnit.Total > 0 || EthereumUnit.Invalid > 0)
                        {
                            if (EthereumUnit.Type == EthereumUnitType.Address)
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter("NaN", System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Public, EthereumUnit.Public > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Private, EthereumUnit.Private > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray)
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                            else
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Invalid, EthereumUnit.Invalid > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Public, EthereumUnit.Public > 0 ? System.Drawing.Color.Orange : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Private, EthereumUnit.Private > 0 ? System.Drawing.Color.BlueViolet : System.Drawing.Color.Gray)
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}/{{3}}/{{4}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region Results

                if (EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) == 0)
                {
                    Log.Print($"The file does not contains valid: {string.Join(" | ", EthereumSearchTypeStorages.Select(Option => Option.Type.GetTag()))}", LogType.WARNING);

                    if (Args.Length > 0)
                    {
                        Args = Array.Empty<string>();
                    }

                    goto GetInput;
                }
                else
                {
                    Formatter[] ProcessingCompleteFormat = new Formatter[]
                    {
                        new Formatter("valid", EthereumSearchTypeStorages.Sum(Unit => Unit.Valid) > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                        new Formatter("invalid", EthereumSearchTypeStorages.Sum(Unit => Unit.Invalid) > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                        new Formatter("duplicates", EthereumSearchTypeStorages.Sum(Unit => Unit.Duplicates) > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                    };

                    Console.WriteFormatted("\n Processing is complete, found {0} / {1} / {2}: ", System.Drawing.Color.White, ProcessingCompleteFormat);

                    foreach (var (EthereumUnit, Index) in EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).WithIndex())
                    {
                        if (EthereumUnit.Total > 0 || EthereumUnit.Invalid > 0)
                        {
                            if (EthereumUnit.Type == EthereumUnitType.Address)
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter("NaN", System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                            else
                            {
                                Formatter[] UnitFormat = new Formatter[]
                                {
                                    new Formatter(EthereumUnit.Valid, EthereumUnit.Valid > 0 ? System.Drawing.Color.GreenYellow : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Invalid, EthereumUnit.Invalid > 0 ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gray),
                                    new Formatter(EthereumUnit.Duplicates, EthereumUnit.Duplicates > 0 ? System.Drawing.Color.Yellow : System.Drawing.Color.Gray),
                                };

                                if ((EthereumSearchTypeStorages.Where(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Total > 0 || EthereumSearchTypeStorage.Invalid > 0).Count() - 1) != Index)
                                {
                                    Console.WriteFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}; ", System.Drawing.Color.White, UnitFormat);
                                }
                                else
                                {
                                    Console.WriteLineFormatted($"{EthereumUnit.Type.GetTag()} - {{0}}/{{1}}/{{2}}\n", System.Drawing.Color.White, UnitFormat);
                                }
                            }
                        }
                    }
                }
                #endregion
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
/*                if (ConsoleQuestion.GetYesNoAnswerStyled("Add private entries to antipublic?", "private", System.Drawing.Color.BlueViolet))
                {*/
                    foreach (var EthereumSearchTypeStorage in EthereumSearchTypeStorages)
                    {
                        if (UniquePrivateUnits.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type).Any())
                        {
                            EthereumSearchTypeStorage.Type.WriteToAp(UniquePrivateUnits.Where(Unit => Unit.Type == EthereumSearchTypeStorage.Type).Select(Unit => Unit.Value).ToArray());
                        }
                    }
/*                }*/
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Public) > 0 && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
                /*                if (ConsoleQuestion.GetYesNoAnswerStyled("Remove public entries?", "public", System.Drawing.Color.Orange))
                                {*/
                //return (Inputs, UniquePrivateUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPrivate);
                /*                }
                                else
                                {*/
                return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.Default);
               // }
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) == 0)
            {
/*                if (ConsoleQuestion.GetYesNoAnswerStyled("All entries are already in the public, do you want to continue?", "public", System.Drawing.Color.Orange))
                {*/
                    return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPublic);
/*                }
                else
                {
                    return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPublicBreakRequested);
                }*/
            }

            if (Settings.Antipublic && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Public) == 0 && EthereumSearchTypeStorages.Sum(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Private) > 0)
            {
                return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.OnlyPrivate);
            }
            else
            {
                return (Inputs, UniqueUnits, InvalidUnits.Distinct(), EthereumParserState.Default);
            }

        }
        private static IEnumerable<string> EnumerateFiles(string Path)
        {
            string[] Files = null;
            string[] Directories = null;

            try
            {
                Files = Directory.GetFiles(Path);
            }
            catch (Exception)
            {
                yield break;
            }

            try
            {
                Directories = Directory.GetDirectories(Path);
            }
            catch (Exception)
            {
                yield break;
            }

            foreach (string File in Files)
            {
                yield return File;
            }

            foreach (string Directory in Directories)
            {
                foreach (string File in EnumerateFiles(Directory))
                {
                    yield return File;
                }
            }
        }
        private static IEnumerable<string> EnumerateAndDecompressArchives(string Path)
        {
            static IEnumerable<string> InternalGetRootPaths(IEnumerable<string> Paths)
            {
                return Paths.Where(Path => !Paths.Any(OtherPath => Path != OtherPath && Path.StartsWith(OtherPath)));
            }

            static IEnumerable<string> InternalEnumerateAndDecompressArchives(string Path)
            {
                int Processed = 0;
                int Total = 0;


                var DecompressedToStack = new ConcurrentStack<string>();
                var UnprocessedPathsStack = new ConcurrentStack<string>();
                UnprocessedPathsStack.Push(Path);

                while (UnprocessedPathsStack.TryPop(out var CurrentPath))
                {
                    var Archives = EnumerateFiles(CurrentPath)
                        .Where(FilePath => EthereumParser.ArchiveFileExtensions.Contains(System.IO.Path.GetExtension(FilePath))).OrderBy(FilePath => new FileInfo(FilePath).Length);

                    Total += Archives.Count();

                    Parallel.ForEach(Archives, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (Archive) =>
                    {
                        try
                        {
                            var Type = System.IO.Path.GetExtension(Archive);
                            var DecompressTo = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Archive), $"{System.IO.Path.GetFileNameWithoutExtension(Archive)}");

                            if (!Directory.Exists(DecompressTo))
                            {
                                Directory.CreateDirectory(DecompressTo);
                            }

                            using (var ConsoleProgress = new ConcurrentConsoleProgress($"[{++Processed}/{Total}] Decompressing archive ----> {Archive}"))
                            {
                                try
                                {
                                    if (Type.Contains(".rar"))
                                    {
                                        using var RarArchive = SharpCompress.Archives.Rar.RarArchive.Open(Archive);

                                        if (RarArchive.TotalSize >= 924288000)
                                        {
                                            return;
                                        }

                                        if (RarArchive.Entries.Where<RarArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                                        {
                                            ConsoleProgress.SetTotal(RarArchive.Entries.Where<RarArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                            foreach (var Entry in RarArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                            {
                                                Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                                {
                                                    ExtractFullPath = false,
                                                    Overwrite = true
                                                });

                                                ConsoleProgress.Report();
                                            }
                                        }

                                    }
                                    else if (Type.Contains(".zip"))
                                    {
                                        using var ZipArchive = SharpCompress.Archives.Zip.ZipArchive.Open(Archive);

                                        if (ZipArchive.TotalSize >= 524288000)
                                        {
                                            return;
                                        }

                                        if (ZipArchive.Entries.Where<ZipArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                                        {
                                            ConsoleProgress.SetTotal(ZipArchive.Entries.Where<ZipArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                            foreach (var Entry in ZipArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                            {
                                                Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                                {
                                                    ExtractFullPath = false,
                                                    Overwrite = true
                                                });

                                                ConsoleProgress.Report();
                                            }
                                        }
                                    }
                                    else if (Type.Contains(".7z"))
                                    {
                                        using var SevenZipArchive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(Archive);

                                        if (SevenZipArchive.TotalSize >= 524288000)
                                        {
                                            return;
                                        }

                                        if (SevenZipArchive.Entries.Where<SevenZipArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                                        {
                                            ConsoleProgress.SetTotal(SevenZipArchive.Entries.Where<SevenZipArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                            foreach (var Entry in SevenZipArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                            {
                                                Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                                {
                                                    ExtractFullPath = false,
                                                    Overwrite = true
                                                });

                                                ConsoleProgress.Report();
                                            }
                                        }

                                    }
                                }
                                catch (CryptographicException)
                                {
                                    ConsoleProgress.Error("Archive encrypted and cannot be processed");
                                    return;
                                }
                                catch (Exception)
                                {
                                    ConsoleProgress.Error("Archive cannot be processed, unpack it manually and try again");
                                    return;
                                }
                            }

                            UnprocessedPathsStack.Push(DecompressTo);
                            DecompressedToStack.Push(DecompressTo);
                        }
                        catch (Exception)
                        {
                            return;
                        }
                    });
                }

                foreach (var DecompressTo in DecompressedToStack)
                {
                    yield return DecompressTo;
                }
            }

            static string InternalEnumerateAndDecompressSingleArchive(string Path)
            {
                var Type = System.IO.Path.GetExtension(Path);
                var DecompressTo = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), $"{System.IO.Path.GetFileNameWithoutExtension(Path)}");

                if (!Directory.Exists(DecompressTo))
                {
                    Directory.CreateDirectory(DecompressTo);
                }

                using (var ConsoleProgress = new ConcurrentConsoleProgress($"[1/1] Decompressing archive ----> {Path}"))
                {
                    try
                    {
                        if (Type.Contains(".rar"))
                        {
                            using var RarArchive = SharpCompress.Archives.Rar.RarArchive.Open(Path);

                            if (RarArchive.Entries.Where<RarArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                            {
                                ConsoleProgress.SetTotal(RarArchive.Entries.Where<RarArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                foreach (var Entry in RarArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                {
                                    Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                    {
                                        ExtractFullPath = false,
                                        Overwrite = true
                                    });

                                    ConsoleProgress.Report();
                                }
                            }
                        }
                        else if (Type.Contains(".zip"))
                        {
                            using var ZipArchive = SharpCompress.Archives.Zip.ZipArchive.Open(Path);

                            if (ZipArchive.Entries.Where<ZipArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                            {
                                ConsoleProgress.SetTotal(ZipArchive.Entries.Where<ZipArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                foreach (var Entry in ZipArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                {
                                    Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                    {
                                        ExtractFullPath = false,
                                        Overwrite = true
                                    });

                                    ConsoleProgress.Report();
                                }
                            }
                        }
                        else if (Type.Contains(".7z"))
                        {
                            using var SevenZipArchive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(Path);

                            if (SevenZipArchive.Entries.Where<SevenZipArchiveEntry>(Entry => !Entry.IsDirectory).Any())
                            {
                                ConsoleProgress.SetTotal(SevenZipArchive.Entries.Where<SevenZipArchiveEntry>(Entry => !Entry.IsDirectory).Count());

                                foreach (var Entry in SevenZipArchive.Entries.Where(Entry => !Entry.IsDirectory))
                                {
                                    Entry.WriteToDirectory(DecompressTo, new ExtractionOptions()
                                    {
                                        ExtractFullPath = false,
                                        Overwrite = true
                                    });

                                    ConsoleProgress.Report();
                                }
                            }
                        }
                    }
                    catch (CryptographicException)
                    {
                        ConsoleProgress.Error("Archive encrypted and cannot be processed");
                        throw;
                    }
                    catch (Exception)
                    {
                        ConsoleProgress.Error("Archive cannot be processed, unpack it manually and try again");
                        throw;
                    }
                }

                InternalEnumerateAndDecompressArchives(DecompressTo);
                return DecompressTo;
            }

            if (File.Exists(Path))
            {
                yield return InternalEnumerateAndDecompressSingleArchive(Path);
            }
            else
            {
                foreach (var RootDecompressedTo in InternalGetRootPaths(new List<string>(InternalEnumerateAndDecompressArchives(Path))))
                {
                    yield return RootDecompressedTo;
                }
            }
        }

        private static List<InputNode> GetInputs(string[] Args, List<EthereumSearchTypeStorage> EthereumSearchTypeStorages, IEnumerable<string> InExtensions, bool DecompressArchives)
        {
            List<InputNode> Inputs = new();

            if (Args.Length == 0)
            {
                try
                {
                    StyleSheet ResultStyleSheet = new StyleSheet(System.Drawing.Color.White);
                    ResultStyleSheet.AddStyle("PATH TO FILE", System.Drawing.Color.Magenta);
                    ResultStyleSheet.AddStyle("DIRECTORY", System.Drawing.Color.Magenta);
                    ResultStyleSheet.AddStyle(" or ", System.Drawing.Color.DarkGray);

                    Console.WriteStyled($"\n Specify the PATH TO FILE or DIRECTORY with: {string.Join(" | ", EthereumSearchTypeStorages.Select(EthereumSearchTypeStorage => EthereumSearchTypeStorage.Type.GetTag()))}: ", ResultStyleSheet);

                    string Path = string.Empty;

                    int CursorTop = Console.CursorTop;
                    int CursorLeft = Console.CursorLeft;

                    while (true)
                    {
                        Path = Console.ReadLine().Replace("\"", string.Empty);

                        if (Path.Length > 0)
                        {
                            break;
                        }
                        else
                        {
                            Console.CursorTop = CursorTop;
                            Console.CursorLeft = CursorLeft;
                            continue;
                        }
                    }

                    Inputs.Add(new(Path));
                }
                catch (Exception Ex)
                {
                    Log.Print(Ex.Message, LogType.ERROR);
                    return GetInputs(Array.Empty<string>(), EthereumSearchTypeStorages, InExtensions, DecompressArchives);
                }
            }
            else
            {
                try
                {
                    foreach (var Arg in Args)
                    {
                        Inputs.Add(new(Arg));
                    }
                }
                catch (Exception Ex)
                {
                    Log.Print(Ex.Message, LogType.ERROR);
                    return GetInputs(Array.Empty<string>(), EthereumSearchTypeStorages, InExtensions, DecompressArchives);
                }
            }

            foreach (var Input in Inputs)
            {
                if (Directory.Exists(Input.Path))
                {
                    Input.AddFiles(
                        EnumerateFiles(Input.Path)
                            .AsParallel()
                            .Where(FilePath => InExtensions.Any(FileExtension => FilePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)))
                            .Where(FilePath => !ForbiddenPathParts.Any(ForbiddenPathPart => FilePath.IndexOf(ForbiddenPathPart, StringComparison.OrdinalIgnoreCase) >= 0))
                            .Select(FilePath => new FileNode(FilePath))
                            .DistinctBy(FileNode => FileNode.Hash)
                            .OrderBy(FileNode => FileNode.Size)
                    );


                    if (DecompressArchives)
                    {
                        var Decompressed = new List<string>(EnumerateAndDecompressArchives(Input.Path));

                        Input.AddFiles(Decompressed.SelectMany(EnumerateFiles)
                                .AsParallel()
                                .Where(FilePath => InExtensions.Any(FileExtension => FilePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)))
                                .Where(FilePath => !ForbiddenPathParts.Any(ForbiddenPathPart => FilePath.IndexOf(ForbiddenPathPart, StringComparison.OrdinalIgnoreCase) >= 0))
                                .Select(FilePath => new FileNode(FilePath))
                                .DistinctBy(FileNode => FileNode.Hash)
                                .ExceptBy(Input.Files.Select(FileNode => FileNode.Hash), FileNode => FileNode.Hash)
                                .OrderBy(FileNode => FileNode.Size)
                        );

                        Input.AddDecompressed(Decompressed);
                    }

                    Console.WriteLine($"\n Directory: {Input.Path} contains unique (by MD5) {Input.Files.Count} files which can be parsed\n");
                }
                else if (File.Exists(Input.Path))
                {
                    if (EthereumParser.ArchiveFileExtensions.Contains(System.IO.Path.GetExtension(Input.Path)))
                    {
                        if (DecompressArchives)
                        {
                            var Decompressed = new List<string>(EnumerateAndDecompressArchives(Input.Path));

                            Input.AddFiles(Decompressed.SelectMany(EnumerateFiles)
                                .AsParallel()
                                .Where(FilePath => InExtensions.Any(FileExtension => FilePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)))
                                .Where(FilePath => !ForbiddenPathParts.Any(ForbiddenPathPart => FilePath.IndexOf(ForbiddenPathPart, StringComparison.OrdinalIgnoreCase) >= 0))
                                .Select(FilePath => new FileNode(FilePath))
                                .DistinctBy(FileNode => FileNode.Hash)
                                );

                            Input.AddDecompressed(Decompressed);
                        }
                    }
                    else
                    {
                        Input.AddFile(new(Input.Path));
                    }
                }
            }

            if (Inputs.Sum(Input => Input.Files.Count) == 0)
            {
                Log.Print($"None of their inputs contains readable files which can be parsed: {string.Join(", ", FileExtensions)}", LogType.ERROR);
                return GetInputs(Array.Empty<string>(), EthereumSearchTypeStorages, InExtensions, DecompressArchives);
            }

            return Inputs;
        }
    }
}
