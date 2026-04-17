using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ClickLockIndicator
{
    public enum OverlayStyle
    {
        None,
        Ring,
        Arc
    }

    public class Settings
    {
        public bool SoundEnabled { get; set; } = false;
        public OverlayStyle OverlayStyle { get; set; } = OverlayStyle.Arc;
        public bool StartWithWindows { get; set; } = false;

        public static string SettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static Settings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new Settings();

                var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var ser = new DataContractJsonSerializer(typeof(Settings));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (Settings)ser.ReadObject(ms);
            }
            catch
            {
                return new Settings();
            }
        }

        public void Save()
        {
            try
            {
                var ser = new DataContractJsonSerializer(typeof(Settings));
                using (var ms = new MemoryStream())
                {
                    ser.WriteObject(ms, this);
                    File.WriteAllText(SettingsPath, Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
            catch { /* best effort */ }
        }
    }
}
