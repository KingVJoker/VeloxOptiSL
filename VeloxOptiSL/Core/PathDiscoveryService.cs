#nullable enable
using System;
using System.IO;
using Microsoft.Win32;

namespace VeloxOptiSL.Core
{
    public static class PathDiscoveryService
    {
        private static readonly string[] CommonPaths = new[]
        {
            @"C:\Program Files\Firestorm-x64\Firestorm-x64.exe",
            @"C:\Program Files (x86)\Firestorm-x64\Firestorm-x64.exe",
            @"C:\Program Files\FirestormOS-x64\FirestormOS-x64.exe"
        };

        /// <summary>
        /// Auto-detects Firestorm-x64.exe path via Windows Registry and fallback install directories.
        /// </summary>
        public static string AutoDetectExecutablePath()
        {
            // 1. Try Windows Registry (HKLM & HKCU Uninstall keys)
            string regPath = SearchRegistry();
            if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
            {
                return regPath;
            }

            // 2. Check standard installation directories
            foreach (string path in CommonPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private static string SearchRegistry()
        {
            string[] registryLocations = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string regLocation in registryLocations)
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(regLocation);
                if (key == null) continue;

                foreach (string subkeyName in key.GetSubKeyNames())
                {
                    using RegistryKey? subkey = key.OpenSubKey(subkeyName);
                    if (subkey == null) continue;

                    string? displayName = subkey.GetValue("DisplayName") as string;
                    if (displayName != null && displayName.Contains("Firestorm", StringComparison.OrdinalIgnoreCase))
                    {
                        string? installLocation = subkey.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(installLocation))
                        {
                            string exePath = Path.Combine(installLocation, "Firestorm-x64.exe");
                            if (File.Exists(exePath)) return exePath;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}