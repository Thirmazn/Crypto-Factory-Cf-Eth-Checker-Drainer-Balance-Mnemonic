using System;
using System.Drawing;
using System.Text;
using System.Threading;

namespace FructoseLib.CLI.Visual.Dynamic
{
    public class ConsoleProgress : IDisposable, IProgress<double>
    {
        private const int BlockCount = 10;
        private readonly TimeSpan AnimationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string Animation = @"|/-\";

        private readonly Timer Timer;

        private double CurrentProgress = 0;
        private string CurrentText = string.Empty;
        private bool Disposed = false;
        private int AnimationIndex = 0;
        public ConsoleProgress()
        {
            Timer = new Timer(TimerHandler);

            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double Value)
        {
            Value = Math.Max(0, Math.Min(1, Value));
            Interlocked.Exchange(ref CurrentProgress, Value);
        }

        private void TimerHandler(object? State)
        {
            lock (Timer)
            {
                if (Disposed) return;

                int ProgressBlockCount = (int)(CurrentProgress * BlockCount);
                int Percent = (int)(CurrentProgress * 100);
                string Text = string.Format("[{0}{1}]{2,3}% {3}",
                    new string('#', ProgressBlockCount), new string('-', BlockCount - ProgressBlockCount),
                    Percent,
                    Animation[AnimationIndex++ % Animation.Length]);
                UpdateText(Text);
                ResetTimer();
            }
        }

        private void UpdateText(string Text)
        {
            int CommonPrefixLength = 0;
            int CommonLength = Math.Min(CurrentText.Length, Text.Length);
            while (CommonPrefixLength < CommonLength && Text[CommonPrefixLength] == CurrentText[CommonPrefixLength])
            {
                CommonPrefixLength++;
            }

            StringBuilder OutputBuilder = new StringBuilder();
            OutputBuilder.Append('\b', CurrentText.Length - CommonPrefixLength);
            OutputBuilder.Append(Text.AsSpan(CommonPrefixLength));

            int OverlapCount = CurrentText.Length - Text.Length;
            if (OverlapCount > 0)
            {
                OutputBuilder.Append(' ', OverlapCount);
                OutputBuilder.Append('\b', OverlapCount);
            }

            Console.Write(OutputBuilder);
            CurrentText = Text;
        }

        private void ResetTimer()
        {
            Timer.Change(AnimationInterval, TimeSpan.FromMilliseconds(-1));
        }


        public void Error(string Text)
        {
            lock (Timer)
            {
                Disposed = true;
                UpdateText(string.Empty);
            }

            Colorful.Console.WriteLine($"[##########] 100% - Error | {Text}", System.Drawing.Color.OrangeRed);
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                lock (Timer)
                {
                    Disposed = true;
                    UpdateText(string.Empty);
                }

                Colorful.Console.WriteLine("[##########] 100% - Done", System.Drawing.Color.DarkGray);
            }
        }

    }

    public class ConcurrentConsoleProgress : IDisposable
    {
        private static object Locker = new object();
        private static object ConstructorLocker = new object();
        private static int ConcurrentInstances = 0;
        public static int LastTopIndex = -1;

        private int BlockCount { get; init; } = 10;
        private int AnimationIndex { get; set; } = 0;
        private TimeSpan AnimationInterval { get; init; } = TimeSpan.FromSeconds(1.0 / 8);
        private Timer Timer { get; init; }
        private string Animation { get; init; } = @"|/-\";
        private bool Disposed { get; set; } = false;
        private (int Left, int Top) StartCoordinates { get; init; }
        private long Total { get; set; } = 0;
        private double CurrentProgress => Math.Max(0, Math.Min(1, (double)Interlocked.Read(ref Processed) / (double)Total));

        private long Processed = 0;
        static ConcurrentConsoleProgress()
        {
            Console.CursorVisible = false;
        }

        public ConcurrentConsoleProgress(string Text, int Total = -1)
        {
            lock (ConstructorLocker)
            {
                if (LastTopIndex + 1 == Console.BufferHeight)
                {
                    while (ConcurrentInstances > 0)
                    {
                        continue;
                    }

                    Console.Clear();
                    LastTopIndex = -1;
                }

                lock (Locker)
                {
                    if (LastTopIndex == -1)
                    {
                        LastTopIndex = Console.GetCursorPosition().Top;
                    }
                    else
                    {
                        LastTopIndex++;
                    }

                    if (ConcurrentInstances > 0)
                    {
                        Console.SetCursorPosition(0, LastTopIndex);
                        Console.Write($" {Text} - ");
                        StartCoordinates = (Console.GetCursorPosition().Left, LastTopIndex);
                    }
                    else
                    {
                        Console.Write($" {Text} - ");
                        this.StartCoordinates = Console.GetCursorPosition();
                    }

                    LastTopIndex = Console.GetCursorPosition().Top;
                    ConcurrentInstances++;
                }
            }

            

            this.Total = Total;
            this.Timer = new Timer(TimerHandler);

            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report()
        {
            if (Total == -1) throw new ArgumentException();
            if (Disposed) throw new ObjectDisposedException(this.GetType().FullName);

            Interlocked.Increment(ref Processed);

/*            if(LastProgress != (int)(CurrentProgress * 100))
            {
                TimerHandler(null);
            }

            Interlocked.Increment(ref LastProgress);*/
        }

        public void SetTotal(long Total)
        {
            lock (Locker)
            {
                if (this.Total == -1)
                {
                    this.Total = Total;
                }
            }
        }

        private void TimerHandler(object? State)
        {
            lock (Locker)
            {
                if (Disposed || Total == -1)
                {
                    return;
                }

                var CurrentProgress = this.CurrentProgress;
                int ProgressBlockCount = (int)(CurrentProgress * BlockCount);
                int Percent = (int)(CurrentProgress * 100);
                string Text = string.Format("[{0}{1}]{2,3}% {3}",
                    new string('#', ProgressBlockCount), new string('-', BlockCount - ProgressBlockCount),
                    Percent,
                    Animation[AnimationIndex++ % Animation.Length]);
                UpdateText(Text);
                ResetTimer();
            }
        }

        private void UpdateText(string Text)
        {
            lock (Locker)
            {
                (int Left, int Top) TempCoordinates = Console.GetCursorPosition();
                Console.SetCursorPosition(StartCoordinates.Left, StartCoordinates.Top);
                Console.Write(Text);
                Console.SetCursorPosition(TempCoordinates.Left, TempCoordinates.Top);
            }
        }

        private void UpdateText(string Text, System.Drawing.Color Color)
        {
            lock (Locker)
            {
                (int Left, int Top) TempCoordinates = Console.GetCursorPosition();
                Console.SetCursorPosition(StartCoordinates.Left, StartCoordinates.Top);
                Colorful.Console.Write(Text, Color);
                Console.SetCursorPosition(TempCoordinates.Left, TempCoordinates.Top);
            }
        }

        private void ResetTimer()
        {
            if (!Disposed)
            {
                Timer.Change(AnimationInterval, TimeSpan.FromMilliseconds(0));
            }
        }

        public void Error(string Text)
        {
            lock (Locker)
            {
                if (!Disposed)
                {
                    UpdateText($"[##########] 100% - Error | {Text}", System.Drawing.Color.OrangeRed);
                    Clear();
                }
            }
        }

        public void Dispose()
        {
            lock (Locker)
            {
                if (!Disposed)
                {
                    UpdateText("[##########] 100% - Done", System.Drawing.Color.DarkGray);
                    Clear();
                }
            }
        }

        private void Clear()
        {
            lock (Locker)
            {
                if (ConcurrentInstances == 1)
                {
                    Console.SetCursorPosition(0, LastTopIndex);
                    LastTopIndex = -1;
                    Console.WriteLine();
                }

                Disposed = true;
                ConcurrentInstances--;
                Timer.Dispose();
                
            }
        }
    }
}
