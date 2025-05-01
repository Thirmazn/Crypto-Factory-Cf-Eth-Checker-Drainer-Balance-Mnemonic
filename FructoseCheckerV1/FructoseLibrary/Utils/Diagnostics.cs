using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FructoseLib.Utils
{
    public static class Diagnostics
    {
        static Diagnostics()
        {
            Timer = new Stopwatch();
        }

        private static Stopwatch Timer { get; set; }
        public static int[] GetThreadsIndex(int Count)
        {
            int[] Array = new int[Count];
            for (int i = 0; i <= Count - 1; i++)
            {
                Array[i] = i + 1;
            }

            return Array;
        }

        public static void InfiniteWait()
        {
            while (true)
            {
                Console.ReadKey();
            }
        }

        public static void StartTimer()
        {
            Timer.Start();
        }
        public static long ElapsedSeconds() { return Timer.ElapsedMilliseconds / 1000; }
        public static long ElapsedMilliseconds() { return Timer.ElapsedMilliseconds; }

    }
}
