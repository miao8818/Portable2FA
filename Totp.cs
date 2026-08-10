using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Portable2FA
{
    public sealed class TotpProfile
    {
        public byte[] Secret { get; set; }
        public string Algorithm { get; set; }
        public int Digits { get; set; }
        public int Period { get; set; }
        public string AccountLabel { get; set; }

        public TotpProfile()
        {
            Algorithm = "SHA1";
            Digits = 6;
            Period = 30;
            AccountLabel = string.Empty;
        }
    }

    public static class Totp
    {
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long UnixTimeSeconds()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static TotpProfile ParseProfile(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new FormatException("请粘贴身份验证密钥");

            string trimmed = input.Trim();
            if (!trimmed.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            {
                return new TotpProfile { Secret = DecodeBase32(trimmed) };
            }

            Uri uri;
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri) ||
                !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase) ||
                !uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("仅支持 TOTP 类型的 otpauth 链接");
            }

            Dictionary<string, string> query = ParseQuery(uri.Query);
            string secret;
            if (!query.TryGetValue("secret", out secret) || string.IsNullOrWhiteSpace(secret))
                throw new FormatException("otpauth 链接中缺少 secret");

            TotpProfile profile = new TotpProfile();
            profile.Secret = DecodeBase32(secret);
            profile.AccountLabel = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

            string value;
            if (query.TryGetValue("algorithm", out value))
            {
                value = value.ToUpperInvariant().Replace("-", string.Empty);
                if (value != "SHA1" && value != "SHA256" && value != "SHA512")
                    throw new FormatException("不支持的算法：" + value);
                profile.Algorithm = value;
            }

            int number;
            if (query.TryGetValue("digits", out value))
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ||
                    (number != 6 && number != 8))
                    throw new FormatException("验证码位数须为 6 或 8");
                profile.Digits = number;
            }

            if (query.TryGetValue("period", out value))
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ||
                    number < 5 || number > 300)
                    throw new FormatException("验证码周期须在 5 到 300 秒之间");
                profile.Period = number;
            }

            return profile;
        }

        public static string Generate(TotpProfile profile, long unixTime)
        {
            if (profile == null || profile.Secret == null || profile.Secret.Length == 0)
                throw new ArgumentException("TOTP 配置无效");

            return Generate(profile.Secret, unixTime, profile.Period, profile.Digits, profile.Algorithm);
        }

        public static string Generate(byte[] secret, long unixTime, int period, int digits, string algorithm)
        {
            long counter = unixTime / period;
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            byte[] hash;
            using (HMAC hmac = CreateHmac(algorithm, secret))
            {
                hash = hmac.ComputeHash(counterBytes);
            }

            int offset = hash[hash.Length - 1] & 0x0F;
            int binary = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

            int divisor = digits == 8 ? 100000000 : 1000000;
            return (binary % divisor).ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }

        public static byte[] DecodeBase32(string value)
        {
            StringBuilder clean = new StringBuilder(value.Length);
            foreach (char original in value)
            {
                char c = char.ToUpperInvariant(original);
                if (char.IsWhiteSpace(c) || c == '-' || c == '=')
                    continue;
                clean.Append(c);
            }

            if (clean.Length == 0)
                throw new FormatException("密钥内容为空");

            List<byte> output = new List<byte>(clean.Length * 5 / 8);
            int buffer = 0;
            int bitsLeft = 0;

            for (int i = 0; i < clean.Length; i++)
            {
                char c = clean[i];
                int value5;
                if (c >= 'A' && c <= 'Z')
                    value5 = c - 'A';
                else if (c >= '2' && c <= '7')
                    value5 = c - '2' + 26;
                else
                    throw new FormatException("密钥包含无效字符：" + c);

                buffer = (buffer << 5) | value5;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output.Add((byte)((buffer >> bitsLeft) & 0xFF));
                    buffer &= (1 << bitsLeft) - 1;
                }
            }

            if (output.Count == 0)
                throw new FormatException("密钥长度不足");

            return output.ToArray();
        }

        private static HMAC CreateHmac(string algorithm, byte[] secret)
        {
            switch ((algorithm ?? "SHA1").ToUpperInvariant())
            {
                case "SHA256": return new HMACSHA256(secret);
                case "SHA512": return new HMACSHA512(secret);
                default: return new HMACSHA1(secret);
            }
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string raw = query.TrimStart('?');
            foreach (string item in raw.Split('&'))
            {
                if (item.Length == 0)
                    continue;
                int equals = item.IndexOf('=');
                string key = equals >= 0 ? item.Substring(0, equals) : item;
                string value = equals >= 0 ? item.Substring(equals + 1) : string.Empty;
                values[Uri.UnescapeDataString(key.Replace("+", " "))] =
                    Uri.UnescapeDataString(value.Replace("+", " "));
            }
            return values;
        }
    }
}
