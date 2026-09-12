#nullable enable
using System;
using System.Diagnostics;

namespace VeloxOptiSL.Core
{
    public class MetricsMonitor : IDisposable
    {
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _ramCounter;

        public MetricsMonitor()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // Dummy read to initialize internal counters

                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _ramCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
                _ramCounter = null;
            }
        }

        public (float CpuUsage, float RamUsagePercentage) GetCurrentMetrics(float totalRamMb = 16384f)
        {
            float cpu = 0f;
            float ramPct = 0f;

            if (_cpuCounter != null)
            {
                cpu = Math.Clamp(_cpuCounter.NextValue(), 0, 100);
            }

            if (_ramCounter != null)
            {
                float availableMb = _ramCounter.NextValue();
                float usedMb = totalRamMb - availableMb;
                ramPct = Math.Clamp((usedMb / totalRamMb) * 100f, 0, 100);
            }

            return (cpu, ramPct);
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }
    }
}