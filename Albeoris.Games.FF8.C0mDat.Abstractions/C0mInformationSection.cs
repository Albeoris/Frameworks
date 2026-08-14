namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>The enemy information and statistics section.</summary>
public sealed class C0mInformationSection : C0mSection
{
    private C0mText _monsterName;
    private Byte[] _statData;

    public C0mInformationSection(C0mText monsterName, Byte[] statData)
        : base(C0mSectionKind.Information)
    {
        ArgumentNullException.ThrowIfNull(monsterName);
        ArgumentNullException.ThrowIfNull(statData);
        _monsterName = monsterName;
        _statData = statData;
    }

    /// <summary>The enemy name stored in the section's fixed 24-byte text field.</summary>
    public C0mText MonsterName
    {
        get => _monsterName;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _monsterName = value;
        }
    }

    /// <summary>The raw stat bytes following the name field.</summary>
    public Byte[] StatData
    {
        get => _statData;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _statData = value;
        }
    }
}
