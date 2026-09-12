#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace VeloxOptiSL.Core
{
    public class UserAppSettings
    {
        public string ExePath { get; set; } = string.Empty;
        public double RenderAvatars { get; set; } = 4;
        public double DrawDistance { get; set; } = 48;
        public double Arc { get; set; } = 50000;
        public double Lod { get; set; } = 1.15;
        public bool JellyDoll { get; set; } = true;
        public bool Impostors { get; set; } = true;
        public bool HighPriority { get; set; } = true;
        public bool CoreAffinity { get; set; } = true;
        public bool MuteGpu { get; set; } = true;
        public bool TextureOptimization { get; set; } = true;
    }

    public struct CacheCleanResult
    {
        public int FilesDeleted;
        public long BytesFreed;
        public string ErrorMessage;
    }

    public static class CacheManager
    {
        private static string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "VeloxOptiSL");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }

        public static UserAppSettings LoadSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<UserAppSettings>(json) ?? new UserAppSettings();
                }
            }
            catch { }

            return new UserAppSettings();
        }

        public static bool SaveSettings(UserAppSettings settings, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetSettingsFilePath(), json);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static CacheCleanResult ClearTargetedCache(bool clearBrowser, bool clearTextures)
        {
            var result = new CacheCleanResult();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            try
            {
                if (clearBrowser)
                {
                    string browserCache = Path.Combine(appData, "Firestorm_x64", "browser_profile", "Cache");
                    CleanDirectoryContents(browserCache, ref result);
                }

                if (clearTextures)
                {
                    string textureCacheAppData = Path.Combine(appData, "Firestorm_x64", "cache");
                    string textureCacheLocalAppData = Path.Combine(localAppData, "Firestorm_x64", "cache");

                    CleanDirectoryContents(textureCacheAppData, ref result);
                    CleanDirectoryContents(textureCacheLocalAppData, ref result);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static void CleanDirectoryContents(string dirPath, ref CacheCleanResult result)
        {
            if (!Directory.Exists(dirPath)) return;

            var dir = new DirectoryInfo(dirPath);

            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    result.BytesFreed += file.Length;
                    file.Delete();
                    result.FilesDeleted++;
                }
                catch { }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                try
                {
                    subDir.Delete(true);
                }
                catch { }
            }
        }
    }
}