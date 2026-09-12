#nullable enable
using System;
using System.IO;

namespace VeloxOptiSL.Core
{
    public static class CrashDumpCleaner
    {
        /// <summary>
        /// Scans and purges crash dumps, stack traces, and old logs to reclaim space.
        /// </summary>
        public static (int FilesRemoved, long BytesFreed, string Log) PurgeCrashDumps()
        {
            int count = 0;
            long bytesFreed = 0;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] searchDirectories = new[]
            {
                Path.Combine(appData, "Firestorm_x64", "logs"),
                Path.Combine(localAppData, "CrashDumps")
            };

            string[] targetExtensions = new[] { "*.dmp", "*.stacktrace", "*.log.old" };

            foreach (string dir in searchDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                foreach (string ext in targetExtensions)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(dir, ext, SearchOption.TopDirectoryOnly);
                        foreach (string filePath in files)
                        {
                            try
                            {
                                FileInfo info = new FileInfo(filePath);
                                bytesFreed += info.Length;
                                info.Delete();
                                count++;
                            }
                            catch { /* Skip locked or inaccessible files */ }
                        }
                    }
                    catch { /* Skip inaccessible folders */ }
                }
            }

            double freedMb = Math.Round(bytesFreed / (1024.0 * 1024.0), 2);
            string summary = count > 0
                ? $"Purged {count} crash dump file(s) freeing {freedMb} MB."
                : "No lingering crash dumps found.";

            return (count, bytesFreed, summary);
        }
    }
}