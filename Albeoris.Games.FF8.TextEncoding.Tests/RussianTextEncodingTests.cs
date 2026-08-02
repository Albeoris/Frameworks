using Xunit;

namespace Albeoris.Games.FF8.TextEncoding.Tests;

public sealed class RussianTextEncodingTests
{
    [Fact]
    public void Decode_ReturnsExpectedText()
    {
        Byte[] bytes = Convert.FromBase64String(TestData.RussianBase64);
        FF8Encoding encoding = FF8Encoding.CreateRussian();

        String text = encoding.GetString(bytes);

        Assert.Equal(TestData.RussianGameText, text);
    }

    [Fact]
    public void Encode_RoundTripsToOriginalBytes()
    {
        Byte[] expectedBytes = Convert.FromBase64String(TestData.RussianBase64);
        FF8Encoding encoding = FF8Encoding.CreateRussian();

        Byte[] bytes = encoding.GetBytes(TestData.RussianGameText);

        Assert.Equal(expectedBytes, bytes);
    }
}
