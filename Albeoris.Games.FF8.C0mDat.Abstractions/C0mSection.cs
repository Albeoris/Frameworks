namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>Base class for parsed and opaque <c>c0m*.dat</c> sections.</summary>
public abstract class C0mSection : IC0mSection
{
    protected C0mSection(C0mSectionKind kind)
    {
        Kind = kind;
    }

    public Int32 Index => (Int32)Kind;

    public C0mSectionKind Kind { get; }
}
