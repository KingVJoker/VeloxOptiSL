#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeloxOptiSL.Core
{
    public class BenchmarkResult
    {
        public double AverageFps { get; set; }
        public double OnePercentLowFps { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalFramesSampled { get; set; }
    }

    public class FrametimeHarness
    {
        private CancellationTokenSource? _cts;
        private readonly List<double> _frameTimesMs = new();
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }

        public event Action<int, int>? OnProgressUpdate; // Passes (ElapsedSeconds, TotalFramesSampled)
        public event Action<BenchmarkResult>? OnBenchmarkCompleted;

        public void StartBenchmark(int durationSeconds = 60)
        {
            if (IsRunning) return;
            IsRunning = true;
            _frameTimesMs.Clear();
            _cts = new CancellationTokenSource();

            Task.Run(() => RunHarnessAsync(durationSeconds, _cts.Token));
        }

        public void StopBenchmark()
        {
            _cts?.Cancel();
        }

        private async Task RunHarnessAsync(int durationSeconds, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var frameStopwatch = Stopwatch.StartNew();
            int elapsedSeconds = 0;

            try
            {
                while (stopwatch.ElapsedMilliseconds < durationSeconds * 1000 && !token.IsCancellationRequested)
                {
                    frameStopwatch.Restart();

                    // High-resolution delay targeting standard rendering cadence sampling (~60 FPS loop check)
                    await Task.Delay(16, token);

                    double frameTimeMs = frameStopwatch.Elapsed.TotalMilliseconds;
                    if (frameTimeMs < 1) frameTimeMs = 1;

                    lock (_lock)
                    {
                        _frameTimesMs.Add(frameTimeMs);
                    }

                    int currentElapsed = (int)(stopwatch.ElapsedMilliseconds / 1000);
                    if (currentElapsed > elapsedSeconds)
                    {
                        elapsedSeconds = currentElapsed;
                        lock (_lock)
                        {
                            OnProgressUpdate?.Invoke(elapsedSeconds, _frameTimesMs.Count);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Benchmark session aborted safely
            }
            finally
            {
                stopwatch.Stop();
                IsRunning = false;

                BenchmarkResult result;
                lock (_lock)
                {
                    result = CalculateResults(stopwatch.Elapsed);
                }

                OnBenchmarkCompleted?.Invoke(result);
            }
        }

        private BenchmarkResult CalculateResults(TimeSpan actualDuration)
        {
            if (_frameTimesMs.Count == 0)
            {
                return new BenchmarkResult { Duration = actualDuration };
            }

            // Average Frametime converted to FPS
            double avgFrameTimeMs = _frameTimesMs.Average();
            double avgFps = 1000.0 / avgFrameTimeMs;

            // 1% Low Calculation: Sort frame times descending (longest frames = lowest performance)
            var sorted = _frameTimesMs.OrderByDescending(ft => ft).ToList();
            int onePercentIndex = Math.Max(1, (int)(sorted.Count * 0.01));

            // Average of the worst 1% frame spikes
            double onePercentLowFrameTimeMs = sorted.Take(onePercentIndex).Average();
            double onePercentLowFps = 1000.0 / onePercentLowFrameTimeMs;

            return new BenchmarkResult
            {
                AverageFps = Math.Round(avgFps, 1),
                OnePercentLowFps = Math.Round(onePercentLowFps, 1),
                Duration = actualDuration,
                TotalFramesSampled = _frameTimesMs.Count
            };
        }
    }
}