using System;
using System.Text;
using Portable2FA;

internal static class TestHarness
{
    private static int failures;

    private static void Main()
    {
        byte[] sha1 = Encoding.ASCII.GetBytes("12345678901234567890");
        byte[] sha256 = Encoding.ASCII.GetBytes("12345678901234567890123456789012");
        byte[] sha512 = Encoding.ASCII.GetBytes(
            "1234567890123456789012345678901234567890123456789012345678901234");

        long[] times = { 59, 1111111109, 1111111111, 1234567890, 2000000000, 20000000000 };
        string[] expected1 = { "94287082", "07081804", "14050471", "89005924", "69279037", "65353130" };
        string[] expected256 = { "46119246", "68084774", "67062674", "91819424", "90698825", "77737706" };
        string[] expected512 = { "90693936", "25091201", "99943326", "93441116", "38618901", "47863826" };

        for (int i = 0; i < times.Length; i++)
        {
            Check("SHA1 vector " + i, expected1[i], Totp.Generate(sha1, times[i], 30, 8, "SHA1"));
            Check("SHA256 vector " + i, expected256[i], Totp.Generate(sha256, times[i], 30, 8, "SHA256"));
            Check("SHA512 vector " + i, expected512[i], Totp.Generate(sha512, times[i], 30, 8, "SHA512"));
        }

        string encoded = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        Check("Base32 decoder", Encoding.ASCII.GetString(sha1),
            Encoding.ASCII.GetString(Totp.DecodeBase32(encoded)));

        TotpProfile raw = Totp.ParseProfile("GEZD GN-BVGY3TQOJQGEZDGNBVGY3TQOJQ");
        Check("Raw defaults", "SHA1/6/30",
            raw.Algorithm + "/" + raw.Digits + "/" + raw.Period);

        TotpProfile uri = Totp.ParseProfile(
            "otpauth://totp/Example%3Auser%40mail.test?secret=" + encoded +
            "&algorithm=SHA256&digits=8&period=45");
        Check("URI label", "Example:user@mail.test", uri.AccountLabel);
        Check("URI options", "SHA256/8/45",
            uri.Algorithm + "/" + uri.Digits + "/" + uri.Period);

        if (failures != 0)
        {
            Console.Error.WriteLine("FAILED: " + failures + " test(s)");
            Environment.Exit(1);
        }

        Console.WriteLine("PASS: 22 checks (RFC 6238 + Base32 + otpauth parsing)");
    }

    private static void Check(string name, string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            failures++;
            Console.Error.WriteLine(name + ": expected " + expected + ", got " + actual);
        }
    }
}
