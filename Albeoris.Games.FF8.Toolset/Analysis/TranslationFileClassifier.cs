using System.Text.RegularExpressions;
using Albeoris.Games.FF8.Toolset.Analysis.Model;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class TranslationFileClassifier
{
    private readonly IReadOnlyList<PatternGroup> groups =
    [
        new(TranslationCategory.Dialogues, ["*/*.msd"]),
        new(TranslationCategory.SystemTextAndUi,
        [
            "*/kernel.bin", "*/mngrp.bin", "*/namedic.bin", "*/tkmnmes*.bin", "*/*.dc1", "*/*.dc2",
        ]),
        new(TranslationCategory.Fonts, ["*/sysfnt*", "*/sysfld*"]),
        new(TranslationCategory.JapaneseFonts,
        [
            "*/sysevn*", "*/sysodd*", "*/font8*", "*/jp_add_font_hd/*",
        ]),
        new(TranslationCategory.TextTextures,
        [
            "*/text*.png", "*/iconfl*", "*/ff8.lzs*.png", "*/start*.tex*", "*/mag*.tex_*.png",
            "*/opening/*", "*/icon.tex*", "*/layout_pc/system/??/*", "*/layout_pc/keyboard/??/*",
            "*/layout_pc/logo/*",
        ]),
        new(TranslationCategory.BattleText, ["*/c0m*.dat", "*/field_bg/gover/*"]),
    ];

    public IReadOnlyList<TranslationCategory> Classify(String path)
    {
        String normalizedPath = Normalize(path);
        List<TranslationCategory> result = [];
        foreach (PatternGroup group in groups)
        {
            if (group.IsMatch(normalizedPath))
                result.Add(group.Category);
        }
        return result;
    }

    internal static String Normalize(String path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private sealed class PatternGroup
    {
        private readonly IReadOnlyList<Regex> patterns;

        public PatternGroup(TranslationCategory category, IReadOnlyList<String> patterns)
        {
            Category = category;
            this.patterns = patterns.Select(CreateRegex).ToArray();
        }

        public TranslationCategory Category { get; }

        public Boolean IsMatch(String path) => patterns.Any(pattern => pattern.IsMatch(path));

        private static Regex CreateRegex(String pattern)
        {
            String expression = Regex.Escape(Normalize(pattern))
                .Replace("\\*", ".*", StringComparison.Ordinal)
                .Replace("\\?", "[^/]", StringComparison.Ordinal);
            return new Regex($"^{expression}$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
