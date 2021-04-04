using System.Diagnostics;
using Dck.Engine.Logging;

namespace Dck.Engine.Graphics.Services
{
    public class Time : Stopwatch
    {
        private double _currentElapsed;
        private double _lastElapsed;

        public Time()
        {
            _lastElapsed = Elapsed.TotalSeconds;
            _currentElapsed = _lastElapsed;
        }

        public float DeltaTime => (float) (_currentElapsed - _lastElapsed);

        public void Update()
        {
            _lastElapsed = _currentElapsed;
            _currentElapsed = Elapsed.TotalSeconds;
            Log.Debug("clock updated");
            Log.Debug($"{_currentElapsed} {_lastElapsed} {Elapsed.TotalSeconds}");
        }

        public new static Time StartNew()
        {
            var stopwatch = new Time();
            stopwatch.Start();
            return stopwatch;
        }
    }
}