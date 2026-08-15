using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class TranslationFileClassifierTests
{
    private readonly TranslationFileClassifier classifier = new();

    [Theory]
    [InlineData(@"main.zzz\data\lang-en\field\event.msd", "Dialogues")]
    [InlineData("main.zzz/data/lang-en/kernel.bin", "SystemTextAndUi")]
    [InlineData("main.zzz/data/menu/sysfnt.tex", "Fonts")]
    [InlineData("main.zzz/data/menu/sysevn00.tex", "JapaneseFonts")]
    [InlineData("main.zzz/data/layout_pc/system/en/title.png", "TextTextures")]
    [InlineData("main.zzz/data/battle/c0m001.dat", "BattleText")]
    public void Classify_MatchingPath_ReturnsExpectedCategory(String path, String expectedName)
    {
        TranslationCategory expected = Enum.Parse<TranslationCategory>(expectedName);
        Assert.Contains(expected, classifier.Classify(path));
    }

    [Fact]
    public void Classify_TrailingWildcardIncludesFolderContents()
    {
        IReadOnlyList<TranslationCategory> result = classifier.Classify(
            "main.zzz/data/menu/jp_add_font_hd/sub/font.tex");

        Assert.Contains(TranslationCategory.JapaneseFonts, result);
    }
}
