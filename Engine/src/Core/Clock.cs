using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dck.Engine.Logging;

namespace Dck.Engine.Core
{
    public class Clock : Stopwatch
    {
        private double _currentElapsed;
        private double _lastElapsed;
        private readonly List<double> _cachedMs = new();
        private readonly int _cacheCapacity = 10;

        private Clock()
        {
            _lastElapsed = Elapsed.TotalSeconds;
            _currentElapsed = _lastElapsed;
        }

        public float DeltaTime => (float) (_currentElapsed - _lastElapsed);
        // ReSharper disable once InconsistentNaming
        public uint FPS { get; private set; } = 0;

        public void Update()
        {
            _lastElapsed = _currentElapsed;
            _currentElapsed = Elapsed.TotalSeconds;
            AddToCache(DeltaTime);
        }

        public new static Clock StartNew()
        {
            var stopwatch = new Clock();
            stopwatch.Start();
            return stopwatch;
        }

        private void AddToCache(double delta)
        {
            _cachedMs.Add(delta);
            if (_cachedMs.Count < _cacheCapacity) return;
            FPS = (uint) Math.Round(1/_cachedMs.Average());
            _cachedMs.Clear();
        }
    }
}