using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Portable2FA
{
    public sealed class AppSettings
    {
        public bool EnableHotkey { get; set; }
        public bool StartWithWindows { get; set; }
        public bool WindowsSync { get; set; }
        public int ShowHotkeyKey { get; set; }
        public int ShowHotkeyModifiers { get; set; }
        public int CaptureHotkeyKey { get; set; }
        public int CaptureHotkeyModifiers { get; set; }

        public AppSettings()
        {
            EnableHotkey = true;
            ShowHotkeyKey = (int)Keys.T;
            ShowHotkeyModifiers = (int)(Keys.Control | Keys.Alt);
            CaptureHotkeyKey = (int)Keys.Q;
            CaptureHotkeyModifiers = (int)(Keys.Control | Keys.Alt);
        }

        public static string SettingsPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "Portable2FA", "settings.json");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();
                string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                AppSettings value = new JavaScriptSerializer().Deserialize<AppSettings>(json);
                return value ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            Directory.CreateDirectory(directory);
            string json = new JavaScriptSerializer().Serialize(this);
            File.WriteAllText(SettingsPath, json, new UTF8Encoding(false));
        }
    }

    internal static class StartupManager
    {
        private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "Portable2FA";

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (enabled)
                {
                    string command = "\"" + System.Windows.Forms.Application.ExecutablePath +
                        "\" --startup";
                    key.SetValue(ValueName, command, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }
    }
}
