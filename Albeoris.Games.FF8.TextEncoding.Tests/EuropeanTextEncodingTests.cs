using Xunit;

namespace Albeoris.Games.FF8.TextEncoding.Tests;

public sealed class EuropeanTextEncodingTests
{
    [Fact]
    public void Decode_ReturnsExpectedText()
    {
        Byte[] bytes = Convert.FromBase64String(TestData.EuropeanBase64);
        FF8Encoding encoding = FF8Encoding.CreateEuropean();

        String text = encoding.GetString(bytes);

        Assert.Equal(TestData.EuropeanText, text);
    }

    [Fact]
    public void Encode_RoundTripsToOriginalBytes()
    {
        Byte[] expectedBytes = Convert.FromBase64String(TestData.EuropeanBase64);
        FF8Encoding encoding = FF8Encoding.CreateEuropean();

        Byte[] bytes = encoding.GetBytes(TestData.EuropeanText);

        Assert.Equal(expectedBytes, bytes);
    }
}
