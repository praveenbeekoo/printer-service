using System;
using Xunit;
using PrinterService;

namespace PrinterService.Tests;

public class EncodingHelperTests
{
    [Fact]
    public void HexStringToBytes_ValidHex_ReturnsBytes()
    {
        var hex = "1B40 48 65 6C 6C 6F";
        var bytes = EncodingHelper.HexStringToBytes(hex);
        Assert.NotNull(bytes);
        Assert.Equal(new byte[] { 0x1B, 0x40, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, bytes);
+    }

    [Fact]
    public void HexStringToBytes_InvalidLength_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => EncodingHelper.HexStringToBytes("ABC"));
    }

    [Fact]
    public void HexStringToBytes_InvalidCharacters_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => EncodingHelper.HexStringToBytes("GG"));
    }

    [Fact]
    public void TryDecodeBase64_Valid_ReturnsTrue()
    {
        var ok = EncodingHelper.TryDecodeBase64("SGVsbG8=", out var bytes);
        Assert.True(ok);
        Assert.Equal("Hello", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void TryDecodeBase64_Invalid_ReturnsFalse()
    {
        var ok = EncodingHelper.TryDecodeBase64("not-base64", out var bytes);
        Assert.False(ok);
        Assert.Null(bytes);
    }
}

