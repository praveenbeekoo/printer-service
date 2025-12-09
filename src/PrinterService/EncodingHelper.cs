using System;
using System.Text;

namespace PrinterService;

public static class EncodingHelper
{
    public static byte[] HexStringToBytes(string hex)
    {
        if (hex is null) throw new ArgumentNullException(nameof(hex));
        var cleaned = hex.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (cleaned.Length % 2 != 0) throw new FormatException("Invalid HEX string length");
        var len = cleaned.Length / 2;
        var bytes = new byte[len];
        for (int i = 0; i < len; i++)
        {
            var segment = cleaned.Substring(i * 2, 2);
            try
            {
                bytes[i] = Convert.ToByte(segment, 16);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Invalid HEX segment '{segment}'", ex);
            }
        }
        return bytes;
    }

    public static bool TryDecodeBase64(string base64, out byte[]? bytes)
    {
        bytes = null;
        if (string.IsNullOrWhiteSpace(base64)) return false;
        try
        {
            bytes = Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

