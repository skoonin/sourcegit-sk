using System;

using Avalonia.Threading;

namespace SourceGit.ViewModels
{
    // Coalesces rapid triggers (e.g. per-keystroke filter changes) into one action after a quiet
    // period, so expensive rebuilds like the stacked diff do not run for every character typed.
    // UI-thread only: both Trigger() and the action run on the Avalonia dispatcher.
    public class Debouncer
    {
        public Debouncer(TimeSpan delay, Action action)
        {
            _timer = new DispatcherTimer { Interval = delay };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                action();
            };
        }

        public void Trigger()
        {
            _timer.Stop();
            _timer.Start();
        }

        private readonly DispatcherTimer _timer;
    }
}
