using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace FructoseCheckerV1.Utils
{
    public enum LogType { OK, MESSAGE, WARNING, ERROR, EXCEPTION, MONEY, UNLICENSED, NOT_INSTALLED }
    static class Log
    {
        private readonly static string LogFile = Path.Join(Path.GetDirectoryName(Imports.GetExecutablePath()), @"\Log.txt");
        public static void Print(string Text, LogType LogType, bool Header = true)
        {
            Color Color = new();
            switch (LogType)
            {
                case LogType.OK:
                    Color = Color.GreenYellow;
                    break;
                case LogType.MESSAGE:
                    Color = Color.White;
                    break;
                case LogType.WARNING:
                    Color = Color.Yellow;
                    break;
                case LogType.ERROR:
                    Color = Color.Red;
                    break;
                case LogType.EXCEPTION:
                    Color = Color.Red;
                    break;
                case LogType.MONEY:
                    Color = Color.Orange;
                    break;
                case LogType.UNLICENSED:
                    Color = Color.Red;
                    break;
                case LogType.NOT_INSTALLED:
                    Color = Color.Red;
                    break;
            }

            if (Header)
            {
                Console.WriteLine($" [{DateTime.Now:dd:MM:yyyy - HH:mm:ss}]:[{LogType}] - {Text}", Color);
            }
            else
            {
                Console.WriteLine($" {Text}", Color);
            }

        }
        public static async Task Error(string Text, LogType LogType)
        {
            await File.AppendAllTextAsync("ErrorLog.txt", $" [{DateTime.Now:dd:MM:yyyy - HH:mm:ss}]:[{LogType}] - {Text}\n");
        }
    }
}
