using System.Reflection;
using Albeoris.Games.Core.NsResources;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsResources;

public class EmbeddedResourcesTests
{
    private static readonly Assembly TestAssembly = typeof(EmbeddedResourcesTests).Assembly;

    [Fact]
    public void Open_ReturnsContent_ForExistingResource()
    {
        using Stream stream = EmbeddedResources.Open(TestAssembly, "/NsResources/Resources/sample.txt");
        using StreamReader reader = new StreamReader(stream);

        String content = reader.ReadToEnd();

        Assert.Equal("sample-content", content);
    }

    [Fact]
    public void Open_ReturnsContent_ForPathWithoutLeadingSlash()
    {
        using Stream stream = EmbeddedResources.Open(TestAssembly, "NsResources/Resources/sample.txt");
        using StreamReader reader = new StreamReader(stream);

        String content = reader.ReadToEnd();

        Assert.Equal("sample-content", content);
    }

    [Fact]
    public void Open_MatchesExactName_NotJustSuffix()
    {
        // "sample.txt" is a suffix of both "sample.txt" and "other.sample.txt",
        // so a naive EndsWith-based search could return the wrong resource.
        using Stream stream = EmbeddedResources.Open(TestAssembly, "/NsResources/Resources/sample.txt");
        using StreamReader reader = new StreamReader(stream);

        String content = reader.ReadToEnd();

        Assert.Equal("sample-content", content);
        Assert.NotEqual("other-content", content);
    }

    [Fact]
    public void Open_Throws_ForMissingResource()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(delegate
        {
            EmbeddedResources.Open(TestAssembly, "/NsResources/Resources/missing.txt");
        });

        Assert.Contains("missing.txt", exception.Message);
    }
}
