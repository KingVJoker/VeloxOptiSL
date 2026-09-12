#nullable enable
using System;
using System.Diagnostics;
using System.IO;

namespace VeloxOptiSL.Core
{
    public static class ProcessManager
    {
        public static (bool Success, string Message, Process? Process) LaunchViewer(
            string exePath,
            bool highPriority,
            bool coreAffinity,
            EventHandler exitCallback)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return (false, "The specified Firestorm executable path does not exist.", null);
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
                    UseShellExecute = true
                };

                Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    return (false, "Failed to initialize process instance.", null);
                }

                // Safely apply High Priority
                if (highPriority)
                {
                    try
                    {
                        process.PriorityClass = ProcessPriorityClass.High;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ProcessManager Warning] Could not set High Priority: {ex.Message}");
                    }
                }

                // Safely apply Core 0 Exclusion Affinity Mask
                if (coreAffinity && Environment.ProcessorCount > 1)
                {
                    try
                    {
                        long affinityMask = 0;
                        int maxCores = Math.Min(Environment.ProcessorCount, 64);
                        for (int i = 1; i < maxCores; i++)
                        {
                            affinityMask |= (1L << i);
                        }
                        if (affinityMask > 0)
                        {
                            process.ProcessorAffinity = (IntPtr)affinityMask;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ProcessManager Warning] Could not set Core Affinity: {ex.Message}");
                    }
                }

                // Attach Passive Crash Observer
                process.EnableRaisingEvents = true;
                process.Exited += exitCallback;

                string statusMsg = $"Viewer launched successfully. PID: {process.Id} (Affinity & Priority enforced).";
                return (true, statusMsg, process);
            }
            catch (Exception ex)
            {
                return (false, $"Launch error: {ex.Message}", null);
            }
        }
    }
}