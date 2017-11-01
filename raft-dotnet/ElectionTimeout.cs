using System;
using System.Threading.Tasks;

namespace raft_dotnet
{
    /// <summary>
    /// Tracks the election timeout and raises an event if Reset is not called before the timeout is reached.
    /// Reset must be called at least once to start tracking the election timeout.
    /// </summary>
    public sealed class ElectionTimeout : IDisposable
    {
        private static readonly Random Rnd = new Random();

        private bool _running;
        private TimeSpan _timeout;
        private DateTime _lastReset;

        /// <summary>
        /// Raised when the election timeout is reached before <see cref="Reset" /> is called.
        /// </summary>
        public event EventHandler<EventArgs> TimeoutReached;

        /// <summary>
        /// Resets the timeout to a random interval between 150 and 300ms
        /// </summary>
        public void Reset()
        {
            var timeout = Rnd.Next(150, 300);
            Reset(TimeSpan.FromMilliseconds(timeout));
        }

        /// <summary>
        /// Resets the timeout and sets the timeout for the next delay to the supplied value.
        /// </summary>
        /// <param name="timeout"></param>
        public void Reset(TimeSpan timeout)
        {
            _timeout = timeout;
            _lastReset = DateTime.UtcNow;
            if (!_running)
            {
                _running = true;
                BackgroundThreadAsync();
            }
        }

        public void Dispose()
        {
            _running = false;
        }

        private async void BackgroundThreadAsync()
        {
            // We check _running just before the event could be raised to avoid events being raised after this class is disposed
            while (_running)
            {
                var waitToNextTimeout = _lastReset + _timeout - DateTime.UtcNow;
                if (waitToNextTimeout <= TimeSpan.Zero)
                {
                    TimeoutReached?.Invoke(this, new EventArgs());
                    _lastReset = DateTime.UtcNow;
                    waitToNextTimeout = _timeout;
                }
                // The above makes sure that the delay cannot be negative
                await Task.Delay(waitToNextTimeout);
            }
        }
    }
}
