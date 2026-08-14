namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>One logical section of a <c>c0m*.dat</c> file.</summary>
public interface IC0mSection
{
    /// <summary>The section's fixed position in the file's section table.</summary>
    Int32 Index { get; }

    /// <summary>The section's binary layout.</summary>
    C0mSectionKind Kind { get; }
}
