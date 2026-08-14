namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>The enemy AI programs and the strings referenced by them during battle.</summary>
public sealed class C0mBattleScriptSection : C0mSection
{
    public C0mBattleScriptSection()
        : base(C0mSectionKind.BattleScript)
    {
    }

    /// <summary>The five AI bytecode programs, without their computed native offsets.</summary>
    public C0mAiScripts AiScripts { get; } = new();

    /// <summary>The null-terminated battle texts, without their computed native offsets.</summary>
    public List<C0mText> Texts { get; } = [];
}
