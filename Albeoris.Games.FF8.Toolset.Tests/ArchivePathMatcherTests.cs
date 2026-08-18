using Albeoris.Games.FF8.Toolset.Extraction;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class ArchivePathMatcherTests
{
    [Fact]
    public void FileNameMask_MatchesAtAnyDepth()
    {
        ArchivePathMatcher matcher = ArchivePathMatcher.Create(["*.bin"]);

        Assert.True(matcher.IsMatch("data/lang-en/kernel.bin"));
        Assert.False(matcher.IsMatch("data/lang-en/kernel.dat"));
    }

    [Fact]
    public void PathMask_MatchesTheFullRelativePath()
    {
        ArchivePathMatcher matcher = ArchivePathMatcher.Create(["data/lang-en/*.bin"]);

        Assert.True(matcher.IsMatch("data/lang-en/kernel.bin"));
        Assert.False(matcher.IsMatch("other/data/lang-en/kernel.bin"));
    }

    [Fact]
    public void RegularExpressionMask_MatchesTheFullRelativePath()
    {
        ArchivePathMatcher matcher = ArchivePathMatcher.Create([@"\^data/.+\.(bin|msd)$\"]);

        Assert.True(matcher.IsMatch("data/lang-en/kernel.bin"));
        Assert.False(matcher.IsMatch("other/data/lang-en/kernel.bin"));
    }
}
