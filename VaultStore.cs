using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Portable2FA
{
    public static class VaultStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes(
            "Portable2FA.LocalVault.v1.2026");

        public static string VaultPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "Portable2FA", "vault.dat");
            }
        }

        public static List<SavedAccount> Load()
        {
            if (!File.Exists(VaultPath))
                return new List<SavedAccount>();

            byte[] encrypted = File.ReadAllBytes(VaultPath);
            return UnprotectAccounts(encrypted);
        }

        public static void Save(List<SavedAccount> accounts)
        {
            byte[] encrypted = ProtectAccounts(accounts);

            string directory = Path.GetDirectoryName(VaultPath);
            Directory.CreateDirectory(directory);
            string temporary = VaultPath + ".tmp";
            File.WriteAllBytes(temporary, encrypted);
            if (File.Exists(VaultPath))
                File.Replace(temporary, VaultPath, null);
            else
                File.Move(temporary, VaultPath);
        }

        public static byte[] ProtectAccounts(List<SavedAccount> accounts)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 1024 * 1024;
            byte[] clear = Encoding.UTF8.GetBytes(serializer.Serialize(accounts));
            try
            {
                return ProtectedData.Protect(clear, Entropy,
                    DataProtectionScope.CurrentUser);
            }
            finally
            {
                Array.Clear(clear, 0, clear.Length);
            }
        }

        public static List<SavedAccount> UnprotectAccounts(byte[] encrypted)
        {
            byte[] clear = ProtectedData.Unprotect(encrypted, Entropy,
                DataProtectionScope.CurrentUser);
            try
            {
                string json = Encoding.UTF8.GetString(clear);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = 1024 * 1024;
                List<SavedAccount> values = serializer.Deserialize<List<SavedAccount>>(json);
                return values ?? new List<SavedAccount>();
            }
            finally
            {
                Array.Clear(clear, 0, clear.Length);
            }
        }
    }
}
