#nullable enable
using System;
using System.Diagnostics;

namespace VeloxOptiSL.Core
{
    public static class ZombieProcessManager
    {
        private static readonly string[] TargetProcessNames = new[]
        {
            "SLPlugin",
            "SLVoice",
            "Firestorm-x64",
            "Firestorm"
        };

        /// <summary>
        /// Kills lingering background processes that might lock settings.xml or cache files.
        /// </summary>
        public static (int KilledCount, string Log) KillZombieProcesses()
        {
            int killed = 0;
            string details = string.Empty;

            foreach (string name in TargetProcessNames)
            {
                Process[] processes = Process.GetProcessesByName(name);
                foreach (Process proc in processes)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(1000);
                        killed++;
                        details += $"• Terminated lingering process: {proc.ProcessName} (PID: {proc.Id})\n";
                    }
                    catch (Exception ex)
                    {
                        details += $"• Failed to terminate {proc.ProcessName}: {ex.Message}\n";
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }

            string summary = killed > 0
                ? $"Cleared {killed} zombie process(es).\n{details}"
                : "No lingering viewer processes detected.";

            return (killed, summary);
        }
    }
}