using System.Text.RegularExpressions;
using Albeoris.Games.FF8.Toolset.Application;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ArchivePathMatcher
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
    private readonly IReadOnlyList<PathPattern> patterns;

    private ArchivePathMatcher(IReadOnlyList<PathPattern> patterns)
    {
        this.patterns = patterns;
    }

    public static ArchivePathMatcher Create(IEnumerable<String> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        List<PathPattern> result = [];
        foreach (String value in values)
        {
            foreach (String part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                result.Add(CreatePattern(part));
        }
        return new ArchivePathMatcher(result);
    }

    public Boolean IsMatch(String relativePath)
    {
        if (patterns.Count == 0)
            return true;

        String normalizedPath = Normalize(relativePath);
        String fileName = Path.GetFileName(normalizedPath);
        return patterns.Any(pattern => pattern.IsMatch(normalizedPath, fileName));
    }

    private static PathPattern CreatePattern(String value)
    {
        if (value.Length >= 2 && value[0] == '\\' && value[^1] == '\\')
        {
            String regexExpression = value[1..^1];
            if (String.IsNullOrWhiteSpace(regexExpression))
                throw new PreparationException("A regular expression mask cannot be empty.");
            return new PathPattern(CreateRegex(regexExpression, value), true);
        }

        String normalized = Normalize(value);
        if (String.IsNullOrWhiteSpace(normalized))
            throw new PreparationException("A file mask cannot be empty.");
        String wildcardExpression = $"^{Regex.Escape(normalized).Replace("\\*", ".*").Replace("\\?", ".")}$";
        return new PathPattern(CreateRegex(wildcardExpression, value), normalized.Contains('/'));
    }

    private static Regex CreateRegex(String expression, String originalMask)
    {
        try
        {
            return new Regex(
                expression,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                MatchTimeout);
        }
        catch (ArgumentException exception)
        {
            throw new PreparationException($"Mask '{originalMask}' is invalid.", exception);
        }
    }

    private static String Normalize(String path) => path.Replace('\\', '/').TrimStart('/');

    private sealed record PathPattern(Regex Regex, Boolean MatchFullPath)
    {
        public Boolean IsMatch(String fullPath, String fileName) => Regex.IsMatch(MatchFullPath ? fullPath : fileName);
    }
}
