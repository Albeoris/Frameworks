using Xunit;

namespace Albeoris.Games.FF8.Jsm.Tests;

public sealed class JsmDocumentRoundTripTests
{
    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Write_ReproducesSourceFileByteForByte(JsmSample sample)
    {
        JsmDocument document = sample.Read();

        Byte[] written = document.Write();

        Assert.Equal(sample.Content, written);
    }

    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Write_IsIdempotentAfterReload(JsmSample sample)
    {
        Byte[] first = sample.Read().Write();

        Byte[] second = Jsm.File.ReadDocument(first).Write();

        Assert.Equal(first, second);
    }
}
