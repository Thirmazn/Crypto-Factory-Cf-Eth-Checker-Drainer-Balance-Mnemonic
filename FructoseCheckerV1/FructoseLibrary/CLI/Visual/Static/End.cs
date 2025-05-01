using Colorful;

using FructoseLib.Utils;

using Console = Colorful.Console;

namespace FructoseLib.CLI.Visual.Static
{
    static partial class ConsoleComponents
    {
        public static void RenderEnd(long Total)
        {
            Formatter[] StartFormat = new Formatter[]
            {
                new Formatter($"{Total}", System.Drawing.Color.BlueViolet),
                new Formatter($"{Diagnostics.ElapsedSeconds() / 60} min {Diagnostics.ElapsedSeconds() % 60} sec", System.Drawing.Color.Gray),
            };

            Console.WriteLineFormatted(" \n Succesfuly drained {0} wallets in {1}", System.Drawing.Color.White, StartFormat);
        }
    }
}
