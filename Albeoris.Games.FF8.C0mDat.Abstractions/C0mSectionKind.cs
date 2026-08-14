namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>The fixed logical position and purpose of a section in a <c>c0m*.dat</c> file.</summary>
public enum C0mSectionKind
{
    Skeleton = 1,
    ModelGeometry = 2,
    ModelAnimation = 3,
    Unknown4 = 4,
    AnimationSequences = 5,
    Unknown6 = 6,
    Information = 7,
    BattleScript = 8,
    Sound = 9,
    SoundMetadata = 10,
    Texture = 11
}
