using Xunit;

namespace Albeoris.Games.FF8.Encoding.Tests;

public sealed class JapaneseTextEncodingTests
{
    [Fact]
    public void Decode_WithFieldCharacters_ReturnsExpectedText()
    {
        Byte[] bytes = Convert.FromBase64String(TestData.JapaneseBase64);
        TextEncoding encoding = TextEncoding.CreateJapanese(new DefaultJapaneseFieldCharacters(), TestData.JapaneseFieldName);

        String text = encoding.GetString(bytes);

        Assert.Equal(TestData.JapaneseText, text);
    }

    [Fact]
    public void Encode_ThenDecode_ReturnsOriginalText()
    {
        // Some kanji are reachable both through the base codepage and through a
        // field's external characters, so re-encoding decoded text is not guaranteed
        // to reproduce the exact original bytes; it must, however, remain stable
        // when decoded again.
        TextEncoding encoding = TextEncoding.CreateJapanese(new DefaultJapaneseFieldCharacters(), TestData.JapaneseFieldName);

        Byte[] bytes = encoding.GetBytes(TestData.JapaneseText);
        String text = encoding.GetString(bytes);

        Assert.Equal(TestData.JapaneseText, text);
    }

    [Fact]
    public void Decode_WithoutFieldCharacters_Throws()
    {
        Byte[] bytes = Convert.FromBase64String(TestData.JapaneseBase64);
        TextEncoding encoding = TextEncoding.CreateJapanese();

        Assert.Throws<InvalidOperationException>(() => encoding.GetString(bytes));
    }
}
