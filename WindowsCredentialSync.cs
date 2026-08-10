using System;
using System.Collections.Generic;
using Windows.Security.Credentials;

namespace Portable2FA
{
    public static class WindowsCredentialSync
    {
        private const string ResourceName = "Portable2FA.TOTP.Account";
        public const int MaximumAccounts = 20;

        public static bool IsAvailable()
        {
            try
            {
                new PasswordVault().RetrieveAll();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<SavedAccount> Pull()
        {
            List<SavedAccount> accounts = new List<SavedAccount>();
            PasswordVault vault = new PasswordVault();
            IReadOnlyList<PasswordCredential> values = vault.RetrieveAll();
            foreach (PasswordCredential credential in values)
            {
                if (!credential.Resource.Equals(ResourceName,
                    StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    credential.RetrievePassword();
                    accounts.Add(SavedAccount.FromCredential(
                        credential.UserName, credential.Password));
                }
                catch (FormatException) { }
            }
            return accounts;
        }

        public static void Push(IEnumerable<SavedAccount> allAccounts)
        {
            List<SavedAccount> active = new List<SavedAccount>();
            List<SavedAccount> deleted = new List<SavedAccount>();
            foreach (SavedAccount account in allAccounts)
            {
                if (account.Deleted)
                    deleted.Add(account);
                else
                    active.Add(account);
            }
            if (active.Count > MaximumAccounts)
                throw new InvalidOperationException("Windows 同步最多支持 20 个账户");

            deleted.Sort(delegate(SavedAccount left, SavedAccount right)
            {
                return string.CompareOrdinal(right.UpdatedAt, left.UpdatedAt);
            });
            List<SavedAccount> synchronized = new List<SavedAccount>(active);
            int tombstoneCapacity = MaximumAccounts - active.Count;
            for (int i = 0; i < deleted.Count && i < tombstoneCapacity; i++)
                synchronized.Add(deleted[i]);

            PasswordVault vault = new PasswordVault();
            Dictionary<string, PasswordCredential> existing =
                new Dictionary<string, PasswordCredential>(StringComparer.OrdinalIgnoreCase);
            foreach (PasswordCredential credential in vault.RetrieveAll())
            {
                if (credential.Resource.Equals(ResourceName,
                    StringComparison.OrdinalIgnoreCase))
                    existing[credential.UserName] = credential;
            }

            HashSet<string> currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SavedAccount account in synchronized)
            {
                currentIds.Add(account.Id);
                PasswordCredential previous;
                if (existing.TryGetValue(account.Id, out previous))
                    vault.Remove(previous);
                vault.Add(new PasswordCredential(ResourceName, account.Id,
                    account.ToCredentialValue()));
            }

            foreach (KeyValuePair<string, PasswordCredential> item in existing)
            {
                if (!currentIds.Contains(item.Key))
                    vault.Remove(item.Value);
            }
        }
    }
}
