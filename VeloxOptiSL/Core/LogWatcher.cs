#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VeloxOptiSL.Core
{
    public class LogWatcher
    {
        private CancellationTokenSource? _cts;

        public event Action<string>? OnLogLineCaptured;
        public event Action<string>? OnWarningDetected;

        public void StartMonitoring(string logFilePath)
        {
            StopMonitoring();
            _cts = new CancellationTokenSource();

            Task.Run(() => MonitorLoop(logFilePath, _cts.Token));
        }

        public void StopMonitoring()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task MonitorLoop(string filePath, CancellationToken ct)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                // Start at the end of the log to track live events only
                stream.Seek(0, SeekOrigin.End);

                while (!ct.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        OnLogLineCaptured?.Invoke(line);

                        // Scan for viewer allocation and session warnings
                        if (line.Contains("OUT_OF_MEMORY", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("texture allocation failed", StringComparison.OrdinalIgnoreCase))
                        {
                            OnWarningDetected?.Invoke("⚠️ Low VRAM / Memory Allocation warning detected in viewer log.");
                        }
                        else if (line.Contains("Disconnected from region", StringComparison.OrdinalIgnoreCase))
                        {
                            OnWarningDetected?.Invoke("ℹ️ Region disconnect detected.");
                        }
                    }
                    else
                    {
                        await Task.Delay(500, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnWarningDetected?.Invoke($"Log Watcher Exception: {ex.Message}");
            }
        }
    }
}