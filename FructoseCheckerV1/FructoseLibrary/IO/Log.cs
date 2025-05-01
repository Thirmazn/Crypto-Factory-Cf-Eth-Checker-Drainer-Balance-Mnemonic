using FructoseLib.Utils;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace FructoseLibrary.IO
{
    public  enum LogType { OK, MESSAGE, WARNING, ERROR, EXCEPTION, GOOD, BAD, GRAB, DONATE, WAIT, TRANSFERED, MONEY, EMPTY, NOT_INSTALLED, INVALID, START, PROCESSING, NOT_MONEY_FOR_FEE, EXCEEDED_TX_WAIT_LIMIT, SWEEPER_DETECTED, ESTIMATE_GAS, SMART_CONTRACT_REVERT, UNCLAIMED, CLAIMED }
    public static class LogTypeExtensions
    {
        public static Color GetColor(this LogType Value) => Value switch
        {
            LogType.OK => Color.GreenYellow,
            LogType.MESSAGE => Color.White,
            LogType.WARNING => Color.Yellow,
            LogType.ERROR => Color.OrangeRed,
            LogType.EXCEPTION => Color.Gray,
            LogType.GOOD => Color.GreenYellow,
            LogType.NOT_INSTALLED => Color.OrangeRed,
            LogType.BAD => Color.White,
            LogType.DONATE => Color.BlueViolet,
            LogType.GRAB => Color.Orange,
            LogType.WAIT => Color.White,
            LogType.TRANSFERED => Color.Orange,
            LogType.MONEY => Color.Orange,
            LogType.EMPTY => Color.White,
            LogType.INVALID => Color.Yellow,
            LogType.START => Color.BlueViolet,
            LogType.PROCESSING => Color.White,
            LogType.NOT_MONEY_FOR_FEE => Color.OrangeRed,
            LogType.EXCEEDED_TX_WAIT_LIMIT => Color.OrangeRed,
            LogType.SWEEPER_DETECTED => Color.OrangeRed,
            LogType.ESTIMATE_GAS => Color.OrangeRed,
            LogType.SMART_CONTRACT_REVERT => Color.OrangeRed,
            LogType.UNCLAIMED => Color.Orange,
            LogType.CLAIMED => Color.White,

            _ => throw new NotSupportedException(),
        };

        public static Color GetBackgroundColor(this LogType Value) => Value switch
        {
            LogType.OK => Color.Black,
            LogType.MESSAGE => Color.Black,
            LogType.WARNING => Color.Black,
            LogType.ERROR => Color.White,
            LogType.EXCEPTION => Color.White,
            LogType.GOOD => Color.Black,
            LogType.NOT_INSTALLED => Color.Black,
            LogType.BAD => Color.Black,
            LogType.DONATE => Color.White,
            LogType.GRAB => Color.Black,
            LogType.WAIT => Color.Black,
            LogType.TRANSFERED => Color.Black,
            LogType.MONEY => Color.Black,
            LogType.UNCLAIMED => Color.Black,
            LogType.CLAIMED => Color.Black,
            LogType.EMPTY => Color.Black,
            LogType.INVALID => Color.Black,
            LogType.START => Color.White,
            LogType.PROCESSING => Color.Black,
            LogType.NOT_MONEY_FOR_FEE => Color.Black,
            LogType.EXCEEDED_TX_WAIT_LIMIT => Color.Black,
            LogType.SWEEPER_DETECTED => Color.Black,
            LogType.ESTIMATE_GAS => Color.Black,
            LogType.SMART_CONTRACT_REVERT => Color.Black,
            _ => throw new NotSupportedException(),
        };
    }
    public enum LogFile { LOG, ERROR, RESULT, RESULT_EMPTY, GOOD, BAD, GRABED, SWEEPER, UNCLAIMED, CLAIMED }
    public static class LogFileExtensions
    {
        private static DateTime Time { get; set; }
        private static string RootDirectory { get; set; }
        private static string LogsDirectory { get; set; }
        private static string LogDirectory { get; set; }

        static LogFileExtensions()
        {
            Time = DateTime.Now;
            RootDirectory = new(Path.GetDirectoryName(Imports.GetExecutablePath()));
            LogsDirectory = Path.Combine(RootDirectory, "Logs");
            LogDirectory = Path.Combine(LogsDirectory, $"Log({Time:dd-MM-yyyy - HH\\h mm\\m ss\\s})");


            if (!Directory.Exists(LogsDirectory))
            {
                Directory.CreateDirectory(LogsDirectory);
            }

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }
        
        public static string GetFile(this LogFile Value) => Value switch
        {
            LogFile.LOG => Path.Combine(LogDirectory, "Log.txt"),
            LogFile.ERROR => "Errors.txt",
            LogFile.RESULT => "Result.txt",
            LogFile.RESULT_EMPTY => "ResultEmpty.txt",
            LogFile.GOOD => "Good.txt",
            LogFile.BAD => "Bad.txt",
            LogFile.GRABED => Path.Combine(LogDirectory, "Grabed.txt"),
            LogFile.SWEEPER => Path.Combine(LogDirectory, "Sweeper.txt"),
            LogFile.UNCLAIMED => "Unclaimed.txt",
            LogFile.CLAIMED => "Claimed.txt",
            _ => throw new NotSupportedException(),
        };
    }
    public enum LogStyle { DEFAULT, ENCHANCED, DEFAULT_NO_HEADER, ENCHANCED_NO_HEADER }
    static class Log
    {
        static Log()
        {
            DefaultBackgroundColor = Console.BackgroundColor;
            PrintLocker = new object();
            LoggingAsyncLockers = new();
            
            foreach (var Value in Enum.GetValues(typeof(LogFile)).Cast<LogFile>())
            {
                LoggingAsyncLockers.Add(Value, new AsyncLock());
            }
        }
        private static Color DefaultBackgroundColor { get; set; }
        private static object PrintLocker { get; set; }
        private static Dictionary<LogFile, AsyncLock> LoggingAsyncLockers { get; set; }
       
        

        public static void Print(string Text, LogType LogType, LogStyle LogStyle = LogStyle.DEFAULT_NO_HEADER)
        {
            lock (PrintLocker)
            {
                switch (LogStyle)
                {
                    case LogStyle.DEFAULT:
                        Console.WriteLine($" [{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.ENCHANCED:
                        Console.Write($" [{DateTime.Now:R}] - ", Color.White);
                        Console.BackgroundColor = LogType.GetColor();
                        Console.Write($" {LogType.ToString().Replace("_", " ")} ", LogType.GetBackgroundColor());
                        Console.BackgroundColor = DefaultBackgroundColor;
                        Console.WriteLine($" - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.DEFAULT_NO_HEADER:
                        Console.WriteLine($" [{LogType.ToString().Replace("_", " ")}] - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.ENCHANCED_NO_HEADER:
                        Console.Write(" ");
                        Console.BackgroundColor = LogType.GetColor();
                        Console.Write($" {LogType.ToString().Replace("_", " ")} ", LogType.GetBackgroundColor());
                        Console.BackgroundColor = DefaultBackgroundColor;
                        Console.WriteLine($" - {Text}", LogType.GetColor());
                        break;
                }
            }
        }
        public static void PrintNoNewLine(string Text, LogType LogType, LogStyle LogStyle = LogStyle.DEFAULT)
        {
            lock (PrintLocker)
            {

                switch (LogStyle)
                {
                    case LogStyle.DEFAULT:
                        Console.Write($" [{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.ENCHANCED:
                        Console.Write($" [{DateTime.Now:R}] - ", Color.White);
                        Console.BackgroundColor = LogType.GetColor();
                        Console.Write($" {LogType.ToString().Replace("_", " ")} ", LogType.GetBackgroundColor());
                        Console.BackgroundColor = DefaultBackgroundColor;
                        Console.Write($" - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.DEFAULT_NO_HEADER:
                        Console.Write($" [{LogType.ToString().Replace("_", " ")}] - {Text}", LogType.GetColor());
                        break;
                    case LogStyle.ENCHANCED_NO_HEADER:
                        Console.Write(" ");
                        Console.BackgroundColor = LogType.GetColor();
                        Console.Write($" {LogType.ToString().Replace("_", " ")} ", LogType.GetBackgroundColor());
                        Console.BackgroundColor = DefaultBackgroundColor;
                        Console.Write($" - {Text}", LogType.GetColor());
                        break;
                }
            }
        }


        public static async Task WriteAsync(string Text, LogFile LogFile, bool Header = true)
        {
            using (await LoggingAsyncLockers[LogFile].LockAsync())
            {
                if (Header)
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}] - {Text}\n");
                }
                else
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                }
            }
        }

        public static async Task WriteAsync(string Text, string To, bool Header = true)
        {
            using (await LoggingAsyncLockers[0].LockAsync())
            {
                if (Header)
                {
                    await File.AppendAllTextAsync(To, $"[{DateTime.Now:R}] - {Text}\n");
                }
                else
                {
                    await File.AppendAllTextAsync(To, $"{Text}\n");
                }
            }
        }

        public static async void WriteAsyncFireForget(string Text, LogFile LogFile, bool Header = true)
        {
            using (await LoggingAsyncLockers[LogFile].LockAsync())
            {
                if (Header)
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}] - {Text}\n");
                }
                else
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                }
            }
        }

        public static async void WriteAsyncFireForgetClassified(string Text, LogFile LogFile, LogType LogType, bool Header = true)
        {
            if (Header)
            {
                using (await LoggingAsyncLockers[LogFile].LockAsync())
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                }
            } else
            {
                using (await LoggingAsyncLockers[LogFile].LockAsync())
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                }
            }

        }

        public static async void WriteAsyncFireForgetClassified(string Text, LogFile[] LogFiles, LogType LogType, bool Header = true)
        {
            foreach(var LogFile in LogFiles)
            {
                if (Header)
                {
                    using (await LoggingAsyncLockers[LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                    }
                }
                else
                {
                    using (await LoggingAsyncLockers[LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                    }
                }
            }
        }

        public static async void WriteAsyncFireForgetClassified(string Text, (LogFile LogFile, bool Header)[] LogFiles, LogType LogType)
        {
            foreach (var LogFile in LogFiles)
            {
                if (LogFile.Header)
                {
                    using (await LoggingAsyncLockers[LogFile.LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                    }
                }
                else
                {
                    using (await LoggingAsyncLockers[LogFile.LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.LogFile.GetFile(), $"{Text}\n");
                    }
                }
            }
        }

        public static async Task WriteAsyncClassified(string Text, LogFile LogFile, LogType LogType, bool Header = true)
        {
            if (Header)
            {
                using (await LoggingAsyncLockers[LogFile].LockAsync())
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                }
            }
            else
            {
                using (await LoggingAsyncLockers[LogFile].LockAsync())
                {
                    await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                }
            }

        }

        public static async Task WriteAsyncClassified(string Text, LogFile[] LogFiles, LogType LogType, bool Header = true)
        {
            foreach (var LogFile in LogFiles)
            {
                if (Header)
                {
                    using (await LoggingAsyncLockers[LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                    }
                }
                else
                {
                    using (await LoggingAsyncLockers[LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.GetFile(), $"{Text}\n");
                    }
                }
            }
        }

        public static async Task WriteAsyncClassified(string Text, (LogFile LogFile, bool Header)[] LogFiles, LogType LogType)
        {
            foreach (var LogFile in LogFiles)
            {
                if (LogFile.Header)
                {
                    using (await LoggingAsyncLockers[LogFile.LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.LogFile.GetFile(), $"[{DateTime.Now:R}]:[{LogType.ToString().Replace("_", " ")}] - {Text}\n");
                    }
                }
                else
                {
                    using (await LoggingAsyncLockers[LogFile.LogFile].LockAsync())
                    {
                        await File.AppendAllTextAsync(LogFile.LogFile.GetFile(), $"{Text}\n");
                    }
                }
            }
        }

    }
}
