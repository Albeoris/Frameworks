using Xunit;

namespace Albeoris.Games.FF8.Encoding.Tests;

public sealed class EuropeanTextEncodingTests
{
    [Fact]
    public void Decode_ReturnsExpectedText()
    {
        Byte[] bytes = Convert.FromBase64String(TestData.EuropeanBase64);
        TextEncoding encoding = TextEncoding.CreateEuropean();

        String text = encoding.GetString(bytes);

        Assert.Equal(TestData.EuropeanText, text);
    }

    [Fact]
    public void Encode_RoundTripsToOriginalBytes()
    {
        Byte[] expectedBytes = Convert.FromBase64String(TestData.EuropeanBase64);
        TextEncoding encoding = TextEncoding.CreateEuropean();

        Byte[] bytes = encoding.GetBytes(TestData.EuropeanText);

        Assert.Equal(expectedBytes, bytes);
    }
}
