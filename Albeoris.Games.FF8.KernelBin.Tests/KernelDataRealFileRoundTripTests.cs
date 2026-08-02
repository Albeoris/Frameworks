using Xunit;

namespace Albeoris.Games.FF8.KernelBin.Tests;

/// <summary>
/// Rough validation test that reads the real, shipped kernel.bin, parses it into a
/// <see cref="KernelData"/> model, writes the model back to memory, and asserts that the
/// resulting bytes are identical to the original file. This proves the reader/writer round
/// trip is lossless for the actual game file, not just synthetic data.
/// </summary>
/// <remarks>
/// Uses a hardcoded path to a real, read-only game file on the developer's machine.
/// This test is temporary and will be removed later.
/// </remarks>
public class KernelDataRealFileRoundTripTests
{
    [Fact]
    public void WriteToStream_AfterReadingRealFile_ProducesIdenticalBytes()
    {
        KernelData data = KernelData.ReadFromArray(KernelDataTestContent.TestContent, KernelDataTestContent.TestContentEncoding);
        
        Byte[] roundTrippedBytes = data.WriteToArray(KernelDataTestContent.TestContentEncoding);

        Assert.Equal(KernelDataTestContent.TestContent.Length, roundTrippedBytes.Length);
        Assert.Equal(KernelDataTestContent.TestContent, roundTrippedBytes);
    }
}