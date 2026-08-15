using System.Text.Json;
using System.Text.Json.Serialization;

namespace Albeoris.Games.FF8.Toolset.Analysis.Reports;

internal static class AnalysisJsonSerializerOptions
{
    public static JsonSerializerOptions Create(Boolean indented)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
