using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;

namespace FructoseLib.CLI.Visual.Dynamic
{
    public class ConsoleTitleProgress : IDisposable
    {
        private string OriginalTitle { get; init; }
        private long Total { get; init; }
        private long Processed { get; set; }
        private long Errors { get; set; }
        private object Locker { get; set; }
        private Stopwatch Timer { get; set; }
        private bool Pausing { get; set; }
        private bool Paused { get; set; }

        private bool Disposed { get; set; }
        private ManualResetEvent PauseEvent { get; init; }
        private CancellationTokenSource PauseEventHandlerTaskCancelationToken { get; init; }

        private double ElapsedMinutes
        {
            get
            {
                return Timer.ElapsedMilliseconds / 1000d / 60d == 0d ? 1d : Timer.ElapsedMilliseconds / 1000d / 60d;
            }
        }
        private double ElapsedSeconds
        {
            get
            {
                return Timer.ElapsedMilliseconds / 1000d == 0d ? 1d : Timer.ElapsedMilliseconds / 1000d;
            }
        }
        private double Speed
        {
            get
            {
                return Math.Round(Processed / ElapsedMinutes, 0);
            }
        }
        private long EstimatedTime
        {
            get
            {
                if (Processed > 0)
                {
                    return 1000 * (long)((Total - Processed) / (Processed / ElapsedSeconds));
                }
                else
                {
                    return 9999000;
                }

            }
        }

        public ConsoleTitleProgress(long Total, bool Pausing = false)
        {
            this.Locker = new();
#pragma warning disable CA1416 // Validate platform compatibility
            this.OriginalTitle = Console.Title;
#pragma warning restore CA1416 // Validate platform compatibility
            this.Total = Total;
            this.Timer = new Stopwatch();
            this.Timer.Start();
            this.Pausing = Pausing;
            this.Disposed = false;
            this.PauseEvent = new ManualResetEvent(true);
            this.PauseEventHandlerTaskCancelationToken = new();
            if (this.Pausing)
            {

                Task.Run(() =>
                {
                    while (!PauseEventHandlerTaskCancelationToken.IsCancellationRequested)
                    {
                        if (Console.KeyAvailable)
                        {
                            ConsoleKeyInfo Key = Console.ReadKey(true);

                            if (Key.Key == ConsoleKey.P)
                            {
                                Pause();
                                PauseEvent.Reset();
                            }
                            else if (Key.Key == ConsoleKey.R)
                            {
                                Resume();
                                PauseEvent.Set();
                            }
                        }
                    }
                });
            }
        }

        public void WaitIfPaused()
        {
            if (Pausing && Paused)
            {
                PauseEvent.WaitOne();
            }
        }

        private void Update()
        {
            var Time = TimeSpan.FromMilliseconds(EstimatedTime);
            Console.Title = $"{OriginalTitle}{(Pausing ? " | Press 'P' for pause" : string.Empty)} | Avg Speed - {Speed} min | Estimated time ~ {(Time.Days > 0 ? $"{Time.Days} days " : string.Empty)}{(Time.Hours > 0 ? $"{Time.Hours} hour " : string.Empty)}{Time.Minutes} min {Time.Seconds} sec | Progress - {Processed}/{Total} [{Math.Round((double)Processed / Total * 100, 1)}%]{(Errors > 0 ? $" | Errors - {Errors}" : string.Empty)}";
        }

        public void Report()
        {
            lock (Locker)
            {
                ++Processed;
                if (!Paused && !Disposed)
                {
                    Update();
                }
            }
        }

        public void Error()
        {
            lock (Locker)
            {
                ++Errors;
                if (!Paused && !Disposed)
                {
                    Update();
                }
            }
        }

        private void Pause()
        {
            if (Pausing && !Disposed)
            {
                lock (Locker)
                {
                    Paused = true;
                    Console.Title = $"{OriginalTitle} | Paused | Press 'R' to resume";
                }
            }
        }

        private void Resume()
        {
            if (Pausing && !Disposed)
            {
                lock (Locker)
                {
                    Paused = false;
                    Update();
                }
            }
        }

        private void Clear()
        {
            Console.Title = OriginalTitle;
        }

        public void Dispose()
        {
            lock (Locker)
            {
                Disposed = true;

                if (Pausing && PauseEventHandlerTaskCancelationToken is not null)
                {
                    PauseEventHandlerTaskCancelationToken.Cancel();
                }

                Clear();
            }
        }
    }
}
