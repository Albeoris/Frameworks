namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>The five bytecode programs in an enemy's battle-script section.</summary>
public sealed class C0mAiScripts
{
    private Byte[] _initialization = [];
    private Byte[] _enemyTurn = [];
    private Byte[] _counterAttack = [];
    private Byte[] _death = [];
    private Byte[] _beforeDyingOrHit = [];

    public Byte[] Initialization
    {
        get => _initialization;
        set => _initialization = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Byte[] EnemyTurn
    {
        get => _enemyTurn;
        set => _enemyTurn = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Byte[] CounterAttack
    {
        get => _counterAttack;
        set => _counterAttack = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Byte[] Death
    {
        get => _death;
        set => _death = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Byte[] BeforeDyingOrHit
    {
        get => _beforeDyingOrHit;
        set => _beforeDyingOrHit = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Returns the scripts in their native order.</summary>
    public IReadOnlyList<Byte[]> InFileOrder =>
    [
        Initialization,
        EnemyTurn,
        CounterAttack,
        Death,
        BeforeDyingOrHit
    ];
}
