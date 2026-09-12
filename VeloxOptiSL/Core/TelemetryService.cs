#nullable enable
using System;
using System.Diagnostics;

namespace VeloxOptiSL.Core
{
    public class TelemetryService : IDisposable
    {
        private PerformanceCounter? _processCpuCounter;
        private PerformanceCounter? _processRamCounter;
        private int _monitoredPid;

        /// <summary>
        /// Binds performance counters directly to the target viewer process ID.
        /// </summary>
        public void AttachToProcess(Process process)
        {
            DisposeCounters();
            _monitoredPid = process.Id;

            try
            {
                string instanceName = GetProcessInstanceName(_monitoredPid);
                if (!string.IsNullOrEmpty(instanceName))
                {
                    _processCpuCounter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);
                    _processRamCounter = new PerformanceCounter("Process", "Working Set - Private", instanceName, true);

                    // Initial dummy read to initialize performance counter baseline
                    _processCpuCounter.NextValue();
                }
            }
            catch
            {
                // Fallback handling if process performance counters fail or lack elevation
                DisposeCounters();
            }
        }

        /// <summary>
        /// Returns (CPU Percentage normalized across logical cores, Private RAM Usage in MB).
        /// </summary>
        public (double CpuUsage, double RamUsageMb) GetProcessMetrics()
        {
            if (_processCpuCounter == null || _processRamCounter == null)
            {
                return (0.0, 0.0);
            }

            try
            {
                float rawCpu = _processCpuCounter.NextValue();
                double normalizedCpu = Math.Round(rawCpu / Environment.ProcessorCount, 1);

                float rawRamBytes = _processRamCounter.NextValue();
                double ramMb = Math.Round(rawRamBytes / (1024.0 * 1024.0), 1);

                return (normalizedCpu, ramMb);
            }
            catch
            {
                return (0.0, 0.0);
            }
        }

        private static string GetProcessInstanceName(int pid)
        {
            var cat = new PerformanceCounterCategory("Process");
            string[] instances = cat.GetInstanceNames();

            foreach (string instance in instances)
            {
                using var cnt = new PerformanceCounter("Process", "ID Process", instance, true);
                try
                {
                    if ((int)cnt.RawValue == pid)
                    {
                        return instance;
                    }
                }
                catch { }
            }
            return string.Empty;
        }

        public void Detach()
        {
            DisposeCounters();
        }

        private void DisposeCounters()
        {
            _processCpuCounter?.Dispose();
            _processCpuCounter = null;
            _processRamCounter?.Dispose();
            _processRamCounter = null;
            _monitoredPid = 0;
        }

        public void Dispose()
        {
            DisposeCounters();
        }
    }
}