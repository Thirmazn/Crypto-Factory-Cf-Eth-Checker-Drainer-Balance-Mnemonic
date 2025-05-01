using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FructoseLib.CLI.Visual.Static
{
    static partial class ConsoleComponents
    {
        const int MF_BYCOMMAND = 0x00000000;
        const int SC_MINIMIZE = 0xF020;
        const int SC_MAXIMIZE = 0xF030;
        const int SC_SIZE = 0xF000;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        public static void ConfigureWindow(double HeightFactor, double WidthFactor, bool Fixed = false)
        {
            Colorful.Console.WindowHeight = Convert.ToInt32(Colorful.Console.LargestWindowHeight / HeightFactor);
            Colorful.Console.WindowWidth = Convert.ToInt32(Colorful.Console.LargestWindowWidth / WidthFactor);
            Colorful.Console.ForegroundColor = System.Drawing.Color.White;
            Console.BufferWidth = 500;

            if (Fixed == true) 
            {
                DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_SIZE, MF_BYCOMMAND);
            }
        }
    }
}
