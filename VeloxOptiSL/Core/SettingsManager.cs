#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VeloxOptiSL.Core
{
    public static class SettingsManager
    {
        public static bool InjectFirestormSettings(
            double drawDistance,
            double maxAvatars,
            double complexity,
            double lodFactor,
            bool impostors,
            bool muteGpu,
            bool textureOptimization,
            out string logMessage)
        {
            logMessage = string.Empty;
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsPath = Path.Combine(appData, "Firestorm_x64", "user_settings", "settings.xml");

                if (!File.Exists(settingsPath))
                {
                    logMessage = "Firestorm settings.xml not found at default path. Launching raw.";
                    return false;
                }

                // 1. Remove Read-Only attribute if previous crash locked the file
                File.SetAttributes(settingsPath, FileAttributes.Normal);

                var doc = XDocument.Load(settingsPath);

                // 2. Update standard and advanced LLSD keys
                UpdateLlsdSetting(doc, "RenderFarClip", "real", drawDistance.ToString(CultureInfo.InvariantCulture));
                UpdateLlsdSetting(doc, "RenderMaxFullAvatars", "integer", ((int)maxAvatars).ToString());
                UpdateLlsdSetting(doc, "AvatarMaxComplexity", "integer", ((int)complexity).ToString());
                UpdateLlsdSetting(doc, "RenderVolumeLODFactor", "real", lodFactor.ToString(CultureInfo.InvariantCulture));
                UpdateLlsdSetting(doc, "RenderAvatarImpostors", "boolean", impostors ? "true" : "false");

                // Advanced Performance & Memory Toggles
                UpdateLlsdSetting(doc, "RenderMuteGpu", "boolean", muteGpu ? "true" : "false");
                UpdateLlsdSetting(doc, "RenderTextureCompression", "boolean", textureOptimization ? "true" : "false");

                // 3. Force immediate disk write buffer flush
                using (FileStream fs = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    doc.Save(fs);
                }

                logMessage = "Successfully injected optimized settings & advanced memory tweaks into Firestorm settings.xml.";
                return true;
            }
            catch (Exception ex)
            {
                logMessage = $"XML Injection warning: {ex.Message}";
                return false;
            }
        }

        private static void UpdateLlsdSetting(XDocument doc, string keyName, string valueType, string value)
        {
            var keyElement = doc.Descendants("key").FirstOrDefault(k => k.Value == keyName);

            if (keyElement != null)
            {
                // Format A: Standard LLSD Map Wrapper
                if (keyElement.NextNode is XElement mapElement && mapElement.Name.LocalName == "map")
                {
                    var valueKey = mapElement.Descendants("key").FirstOrDefault(k => k.Value == "Value");
                    if (valueKey?.NextNode is XElement valNode)
                    {
                        valNode.Value = value;
                        return;
                    }
                }
                // Format B: Direct Element
                else if (keyElement.NextNode is XElement directNode)
                {
                    directNode.Value = value;
                    return;
                }
            }

            // Format C: Missing Key Injection
            var rootMap = doc.Root?.Element("map") ?? doc.Root;
            if (rootMap != null)
            {
                var newEntry = new XElement("key", keyName);
                var newMap = new XElement("map",
                    new XElement("key", "Comment"),
                    new XElement("string", "Injected by VeloxOpti SL"),
                    new XElement("key", "Value"),
                    new XElement(valueType, value)
                );
                rootMap.Add(newEntry);
                rootMap.Add(newMap);
            }
        }
    }
}