using Colorful;
using FructoseLib.Utils;
using System.Drawing;
using Console = Colorful.Console;

namespace FructoseLib.CLI.Visual.Static
{
    static partial class ConsoleComponents
    {
        public static void RenderStart(int Threads)
        {
            Formatter[] StartFormat = new Formatter[]
            {
                new Formatter($"{Threads}", System.Drawing.Color.BlueViolet),
            };

            Console.WriteLineFormatted(" Start checking on {0} threads...\n", System.Drawing.Color.White, StartFormat);
            Diagnostics.StartTimer();
        }
    }
}
