using System;
using System.Collections.Generic;
using System.Drawing;
using ZXing;
using ZXing.Common;

namespace Portable2FA
{
    public static class QrCodeDecoder
    {
        public static string Decode(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            BarcodeReader reader = new BarcodeReader();
            reader.AutoRotate = true;
            reader.Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            };

            Result result = reader.Decode(bitmap);
            if (result == null || string.IsNullOrWhiteSpace(result.Text))
                throw new System.FormatException("未在图片中识别到二维码");

            string value = result.Text.Trim();
            if (!value.StartsWith("otpauth://totp/", StringComparison.OrdinalIgnoreCase))
                throw new System.FormatException("二维码中不是 TOTP 身份验证链接");

            Totp.ParseProfile(value);
            return value;
        }
    }
}
