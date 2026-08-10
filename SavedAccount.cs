using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Portable2FA
{
    public sealed class SavedAccount
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Issuer { get; set; }
        public string Account { get; set; }
        public string Secret { get; set; }
        public string Algorithm { get; set; }
        public int Digits { get; set; }
        public int Period { get; set; }
        public string UpdatedAt { get; set; }
        public bool Deleted { get; set; }

        public SavedAccount()
        {
            Id = Guid.NewGuid().ToString("N");
            Label = string.Empty;
            Issuer = string.Empty;
            Account = string.Empty;
            Secret = string.Empty;
            Algorithm = "SHA1";
            Digits = 6;
            Period = 30;
            UpdatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Label))
                    return Label;
                return string.IsNullOrWhiteSpace(Issuer) ? Account : Issuer;
            }
        }

        public string Subtitle
        {
            get
            {
                string detail = string.Empty;
                if (!string.IsNullOrWhiteSpace(Issuer) &&
                    !Issuer.Equals(DisplayName, StringComparison.OrdinalIgnoreCase))
                    detail = Issuer;
                if (!string.IsNullOrWhiteSpace(Account))
                    detail += (detail.Length == 0 ? string.Empty : "  ·  ") + Account;
                return detail.Length == 0 ? "TOTP 账户" : detail;
            }
        }

        public TotpProfile ToProfile()
        {
            return new TotpProfile
            {
                Secret = Totp.DecodeBase32(Secret),
                Algorithm = Algorithm,
                Digits = Digits,
                Period = Period,
                Issuer = Issuer,
                AccountLabel = BuildLabel()
            };
        }

        public string ToOtpAuthUri()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("otpauth://totp/");
            builder.Append(Uri.EscapeDataString(BuildLabel()));
            builder.Append("?secret=");
            builder.Append(Uri.EscapeDataString(Secret));
            if (!string.IsNullOrWhiteSpace(Issuer))
            {
                builder.Append("&issuer=");
                builder.Append(Uri.EscapeDataString(Issuer));
            }
            builder.Append("&algorithm=");
            builder.Append(Uri.EscapeDataString(Algorithm));
            builder.Append("&digits=");
            builder.Append(Digits.ToString(CultureInfo.InvariantCulture));
            builder.Append("&period=");
            builder.Append(Period.ToString(CultureInfo.InvariantCulture));
            builder.Append("&x-updated=");
            builder.Append(Uri.EscapeDataString(UpdatedAt));
            if (!string.IsNullOrWhiteSpace(Label))
            {
                builder.Append("&x-label=");
                builder.Append(Uri.EscapeDataString(Label));
            }
            return builder.ToString();
        }

        public string ToCredentialValue()
        {
            if (Deleted)
                return "portable2fa://deleted?updated=" + Uri.EscapeDataString(UpdatedAt);
            return ToOtpAuthUri();
        }

        public static SavedAccount FromProfile(TotpProfile profile, string label,
            string issuer, string account, string existingId)
        {
            SavedAccount item = new SavedAccount();
            if (!string.IsNullOrWhiteSpace(existingId))
                item.Id = existingId;
            item.Label = (label ?? string.Empty).Trim();
            item.Issuer = (issuer ?? string.Empty).Trim();
            item.Account = (account ?? string.Empty).Trim();
            item.Secret = Totp.EncodeBase32(profile.Secret);
            item.Algorithm = profile.Algorithm;
            item.Digits = profile.Digits;
            item.Period = profile.Period;
            item.UpdatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            item.Deleted = false;
            return item;
        }

        public static SavedAccount FromCredential(string id, string uri)
        {
            if (uri.StartsWith("portable2fa://deleted", StringComparison.OrdinalIgnoreCase))
            {
                SavedAccount deleted = new SavedAccount();
                deleted.Id = id;
                deleted.Deleted = true;
                string deletedAt = GetQueryValue(uri, "updated");
                if (!string.IsNullOrWhiteSpace(deletedAt))
                    deleted.UpdatedAt = deletedAt;
                return deleted;
            }

            TotpProfile profile = Totp.ParseProfile(uri);
            string account = profile.AccountLabel;
            int separator = account.IndexOf(':');
            if (separator >= 0)
                account = account.Substring(separator + 1).Trim();
            string label = GetQueryValue(uri, "x-label");
            if (string.IsNullOrWhiteSpace(label))
                label = string.IsNullOrWhiteSpace(profile.Issuer) ? account : profile.Issuer;
            SavedAccount value = FromProfile(profile, label, profile.Issuer, account, id);
            string updatedAt = GetQueryValue(uri, "x-updated");
            if (!string.IsNullOrWhiteSpace(updatedAt))
                value.UpdatedAt = updatedAt;
            return value;
        }

        public static List<SavedAccount> Merge(IEnumerable<SavedAccount> local,
            IEnumerable<SavedAccount> remote)
        {
            Dictionary<string, SavedAccount> values =
                new Dictionary<string, SavedAccount>(StringComparer.OrdinalIgnoreCase);
            AddLatest(values, local);
            AddLatest(values, remote);
            return new List<SavedAccount>(values.Values);
        }

        private static void AddLatest(Dictionary<string, SavedAccount> values,
            IEnumerable<SavedAccount> items)
        {
            if (items == null)
                return;
            foreach (SavedAccount item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                    continue;
                SavedAccount current;
                if (!values.TryGetValue(item.Id, out current) ||
                    string.CompareOrdinal(item.UpdatedAt, current.UpdatedAt) > 0)
                    values[item.Id] = item;
            }
        }

        private string BuildLabel()
        {
            if (string.IsNullOrWhiteSpace(Issuer))
                return string.IsNullOrWhiteSpace(Account) ? "TOTP" : Account;
            if (string.IsNullOrWhiteSpace(Account))
                return Issuer;
            return Issuer + ":" + Account;
        }

        private static string GetQueryValue(string uriText, string requestedKey)
        {
            Uri uri;
            if (!Uri.TryCreate(uriText, UriKind.Absolute, out uri))
                return string.Empty;
            string query = uri.Query.TrimStart('?');
            foreach (string item in query.Split('&'))
            {
                int equals = item.IndexOf('=');
                string key = equals >= 0 ? item.Substring(0, equals) : item;
                if (!key.Equals(requestedKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                string value = equals >= 0 ? item.Substring(equals + 1) : string.Empty;
                return Uri.UnescapeDataString(value.Replace("+", " "));
            }
            return string.Empty;
        }
    }
}
