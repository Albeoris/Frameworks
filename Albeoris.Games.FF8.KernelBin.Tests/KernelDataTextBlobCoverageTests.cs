using System.Text;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.KernelBin.Internal;
using Albeoris.Games.FF8.KernelBin.Internal.Raw;
using Xunit;

namespace Albeoris.Games.FF8.KernelBin.Tests;

/// <summary>
/// Verifies that every byte of every text blob section in the kernel.bin is reachable
/// from some record's Name/Description/AttackName/AttackDescription offset (i.e. no orphaned,
/// unaddressed text data was silently dropped by the reader). Any byte that is not reachable
/// from a known offset must be zero (either a string's null terminator or the writer's
/// trailing alignment padding).
/// </summary>
public class KernelDataTextBlobCoverageTests
{
    [Fact]
    public void AllTextBlobBytes_AreReachableFromAnOffsetOrAreZeroPadding()
    {
        Byte[] content = KernelDataTestContent.TestContent;
        KernelSectionReader reader = new KernelSectionReader(content);
        Encoding encoding = FF8Encoding.CreateJapanese();

        BattleCommandRaw[] battleCommandsRaw = reader.ReadStructArray<BattleCommandRaw>();
        MagicRaw[] magicRaw = reader.ReadStructArray<MagicRaw>();
        GuardianRaw[] guardiansRaw = reader.ReadStructArray<GuardianRaw>();
        EnemyAttackRaw[] enemyAttacksRaw = reader.ReadStructArray<EnemyAttackRaw>();
        WeaponRaw[] weaponsRaw = reader.ReadStructArray<WeaponRaw>();
        SquallLimitRaw[] squallLimitsRaw = reader.ReadStructArray<SquallLimitRaw>();
        CharacterRaw[] charactersRaw = reader.ReadStructArray<CharacterRaw>();
        BattleItemRaw[] battleItemsRaw = reader.ReadStructArray<BattleItemRaw>();
        FieldItemRaw[] fieldItemsRaw = reader.ReadStructArray<FieldItemRaw>();
        IndependentGuardianAttackRaw[] independentGuardianAttacksRaw = reader.ReadStructArray<IndependentGuardianAttackRaw>();
        AdditionalCommandRaw[] additionalCommandsRaw = reader.ReadStructArray<AdditionalCommandRaw>();
        JunctionAbilityRaw[] junctionAbilitiesRaw = reader.ReadStructArray<JunctionAbilityRaw>();
        CommandAbilityRaw[] commandAbilitiesRaw = reader.ReadStructArray<CommandAbilityRaw>();
        CharacterStatAbilityRaw[] characterStatAbilitiesRaw = reader.ReadStructArray<CharacterStatAbilityRaw>();
        CharacterAbilityRaw[] characterAbilitiesRaw = reader.ReadStructArray<CharacterAbilityRaw>();
        PartyAbilityRaw[] partyAbilitiesRaw = reader.ReadStructArray<PartyAbilityRaw>();
        GuardianAbilityRaw[] guardianAbilitiesRaw = reader.ReadStructArray<GuardianAbilityRaw>();
        MenuAbilityRaw[] menuAbilitiesRaw = reader.ReadStructArray<MenuAbilityRaw>();
        NpcLimitRaw[] npcLimitsRaw = reader.ReadStructArray<NpcLimitRaw>();
        BlueMagicRaw[] blueMagicsRaw = reader.ReadStructArray<BlueMagicRaw>();
        QuistisLimitRaw[] quistisLimitsRaw = reader.ReadStructArray<QuistisLimitRaw>();
        IrvinLimitRaw[] irvinLimitsRaw = reader.ReadStructArray<IrvinLimitRaw>();
        ZellLimitRaw[] zellLimitsRaw = reader.ReadStructArray<ZellLimitRaw>();
        ZellDuelMoveRaw[] zellDuelMovesRaw = reader.ReadStructArray<ZellDuelMoveRaw>();
        RinoaLimitRaw[] rinoaLimitsRaw = reader.ReadStructArray<RinoaLimitRaw>();
        RinoaAngeloAttackRaw[] rinoaAngeloAttacksRaw = reader.ReadStructArray<RinoaAngeloAttackRaw>();
        Byte[] selphieSlotIdsRaw = reader.ReadStructArray<Byte>();
        SelphieSlotSetRaw[] selphieSlotSetsRaw = reader.ReadStructArray<SelphieSlotSetRaw>();
        DevourEffectRaw[] devourEffectsRaw = reader.ReadStructArray<DevourEffectRaw>();
        TimerSettingsRaw[] timerSettingsRaw = reader.ReadStructArray<TimerSettingsRaw>();
        UInt16[] miscPointers = reader.ReadStructArray<UInt16>();

        // Sections without any associated text blob; only read here to keep the reader's
        // internal section index in sync with KernelData.ReadFromStream.
        _ = additionalCommandsRaw;
        _ = quistisLimitsRaw;
        _ = zellDuelMovesRaw;
        _ = selphieSlotIdsRaw;
        _ = selphieSlotSetsRaw;
        _ = timerSettingsRaw;

        KernelTextBlobReader battleCommandsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader magicText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader guardiansText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader enemyAttacksText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader weaponsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader squallLimitsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader charactersText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader battleItemsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader fieldItemsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader independentGuardianAttacksText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader junctionAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader commandAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader characterStatAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader characterAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader partyAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader guardianAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader menuAbilitiesText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader npcLimitsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader blueMagicsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader irvinLimitsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader zellLimitsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader rinoaLimitsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader rinoaAngeloAttacksText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader devourEffectsText = reader.ReadTextBlob(encoding);
        KernelTextBlobReader miscText = reader.ReadTextBlob(encoding);

        AssertFullyCovered("battleCommands", battleCommandsText, battleCommandsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("magic", magicText, magicRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("guardians", guardiansText, guardiansRaw, r => [unchecked((UInt16)r.OffsetAttackName), unchecked((UInt16)r.OffsetAttackDescription)]);
        AssertFullyCovered("enemyAttacks", enemyAttacksText, enemyAttacksRaw, r => [r.OffsetName]);
        AssertFullyCovered("weapons", weaponsText, weaponsRaw, r => [r.OffsetName]);
        AssertFullyCovered("squallLimits", squallLimitsText, squallLimitsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("characters", charactersText, charactersRaw, r => [r.OffsetName]);
        AssertFullyCovered("battleItems", battleItemsText, battleItemsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("fieldItems", fieldItemsText, fieldItemsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("independentGuardianAttacks", independentGuardianAttacksText, independentGuardianAttacksRaw, r => [r.OffsetAttackName]);
        AssertFullyCovered("junctionAbilities", junctionAbilitiesText, junctionAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("commandAbilities", commandAbilitiesText, commandAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("characterStatAbilities", characterStatAbilitiesText, characterStatAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("characterAbilities", characterAbilitiesText, characterAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("partyAbilities", partyAbilitiesText, partyAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("guardianAbilities", guardianAbilitiesText, guardianAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("menuAbilities", menuAbilitiesText, menuAbilitiesRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("npcLimits", npcLimitsText, npcLimitsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("blueMagics", blueMagicsText, blueMagicsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("irvinLimits", irvinLimitsText, irvinLimitsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("zellLimits", zellLimitsText, zellLimitsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("rinoaLimits", rinoaLimitsText, rinoaLimitsRaw, r => [r.OffsetName, r.OffsetDescription]);
        AssertFullyCovered("rinoaAngeloAttacks", rinoaAngeloAttacksText, rinoaAngeloAttacksRaw, r => [r.OffsetName]);
        AssertFullyCovered("devourEffects", devourEffectsText, devourEffectsRaw, r => [r.OffsetDescription]);
        AssertFullyCoveredPointers("miscText", miscText, miscPointers);
    }

    private static void AssertFullyCovered<T>(String sectionName, KernelTextBlobReader text, T[] records, Func<T, UInt16[]> selectOffsets)
    {
        Byte[] blob = text.Blob;
        Boolean[] reachable = new Boolean[blob.Length];

        foreach (T record in records)
        {
            foreach (UInt16 offset in selectOffsets(record))
                MarkReachable(reachable, blob, offset);
        }

        AssertNoOrphanBytes(sectionName, blob, reachable);
    }

    private static void AssertFullyCoveredPointers(String sectionName, KernelTextBlobReader text, UInt16[] offsets)
    {
        Byte[] blob = text.Blob;
        Boolean[] reachable = new Boolean[blob.Length];

        foreach (UInt16 offset in offsets)
            MarkReachable(reachable, blob, offset);

        AssertNoOrphanBytes(sectionName, blob, reachable);
    }

    private static void MarkReachable(Boolean[] reachable, Byte[] blob, UInt16 offset)
    {
        if (offset == UInt16.MaxValue)
            return;

        Int32 index = offset;
        while (index < blob.Length && blob[index] != 0)
        {
            reachable[index] = true;
            index++;
        }

        if (index < blob.Length)
            reachable[index] = true;
    }

    private static void AssertNoOrphanBytes(String sectionName, Byte[] blob, Boolean[] reachable)
    {
        for (Int32 i = 0; i < blob.Length; i++)
        {
            if (!reachable[i])
                Assert.True(blob[i] == 0, $"Section '{sectionName}': unreachable non-zero byte at blob offset {i} (value {blob[i]}).");
        }
    }
}
