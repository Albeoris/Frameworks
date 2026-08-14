using System.Buffers.Binary;
using Xunit;

namespace Albeoris.Games.FF8.C0mDat.Tests;

public class C0mFileValidationTests
{
    [Fact]
    public void Read_RejectsAStoredFileSizeThatDoesNotReachTheEnd()
    {
        Byte[] content = C0mSample.European.Content.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(48, sizeof(UInt32)), (UInt32)(content.Length - 1));

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => C0mFile.Read(content, C0mSample.European.NewEncoding()));

        Assert.Contains("file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_RejectsAnOutOfOrderSectionPosition()
    {
        Byte[] content = C0mSample.European.Content.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(8, sizeof(UInt32)), 51);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => C0mFile.Read(content, C0mSample.European.NewEncoding()));

        Assert.Contains("Section 2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_RejectsAMonsterNameWithoutTerminatorSpace()
    {
        C0mFile file = C0mSample.European.Read();
        file.Information.MonsterName.Value = new String('A', 24);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => file.Write());

        Assert.Contains("null terminator", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_RejectsAnUnalignedAiScriptWithoutAStopOpcode()
    {
        C0mFile file = C0mSample.European.Read();
        file.BattleScript.AiScripts.Initialization = [1];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => file.Write());

        Assert.Contains("AI script 0", exception.Message, StringComparison.Ordinal);
    }
}
