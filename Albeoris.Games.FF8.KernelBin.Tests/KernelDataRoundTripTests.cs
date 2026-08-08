using System.Text;
using Albeoris.Games.FF8.KernelBin.Abstractions;
using Albeoris.Games.FF8.TextEncoding;
using Xunit;

namespace Albeoris.Games.FF8.KernelBin.Tests;

/// <summary>
/// Verifies that a synthetically built <see cref="KernelData"/> (populated with one
/// representative entry per section) survives a full write/read round trip, and that
/// editing a single field after reading it back and rewriting it works as expected.
/// </summary>
public class KernelDataRoundTripTests
{
    [Fact]
    public void WriteThenRead_RoundTrip_PreservesAllSections()
    {
        FF8Encoding encoding = FF8Encoding.CreateJapanese();
        KernelData original = CreateSyntheticData();

        KernelData roundTripped = WriteThenRead(original, encoding);

        AssertEqual(original, roundTripped);
    }

    [Fact]
    public void WriteThenRead_ThenEditAndRewrite_PersistsTheEdit()
    {
        FF8Encoding encoding = FF8Encoding.CreateJapanese();
        KernelData original = CreateSyntheticData();
        KernelData data = WriteThenRead(original, encoding);

        data.BattleCommands[1].Description = "セツメイ２６";

        KernelData edited = WriteThenRead(data, encoding);

        Assert.Equal("セツメイ２６", edited.BattleCommands[1].Description);
        Assert.Equal(data.BattleCommands[0].Name, edited.BattleCommands[0].Name);
        Assert.Equal(data.FieldItems[0].Name, edited.FieldItems[0].Name);
    }

    private static KernelData WriteThenRead(KernelData data, Encoding encoding)
    {
        Byte[] content = data.WriteToArray(encoding);
        return KernelData.ReadFromArray(content, encoding);
    }

    private static KernelData CreateSyntheticData()
    {
        KernelData data = new KernelData();

        BattleCommand attack = new BattleCommand();
        attack.Name = "テスト０１";
        attack.Description = "セツメイ０１";
        attack.AbilityId = 1;
        attack.Target = 2;
        attack.Unknown1 = 3;
        attack.Unknown2 = 4;
        data.BattleCommands.Add(attack);

        BattleCommand blank = new BattleCommand();
        blank.Name = null;
        blank.Description = null;
        blank.AbilityId = 0;
        blank.Target = 0;
        blank.Unknown1 = 0;
        blank.Unknown2 = 0;
        data.BattleCommands.Add(blank);

        MagicSpell fire = new MagicSpell();
        fire.Name = "テスト０２";
        fire.Description = "セツメイ０２";
        fire.MagicId = 1;
        fire.Unknown1 = 2;
        fire.AttackType = 3;
        fire.SpellPower = 4;
        fire.Unknown2 = 5;
        fire.DefaultTarget = 6;
        fire.Flags = 7;
        fire.DrawResist = 8;
        fire.HitCount = 9;
        fire.Element = Element.Fire;
        fire.Unknown4 = 10;
        fire.StatusMagic1 = 11;
        fire.StatusMagic2 = 12;
        fire.StatusMagic3 = 13;
        fire.StatusMagic4 = 14;
        fire.StatusMagic5 = 15;
        fire.Unknown5 = 16;
        fire.StatusAttack = 17;
        fire.HP = 18;
        fire.STR = 19;
        fire.VIT = 20;
        fire.MAG = 21;
        fire.SPR = 22;
        fire.SPD = 23;
        fire.EVA = 24;
        fire.HIT = 25;
        fire.LUCK = 26;
        fire.ElemAttackEnabled = 27;
        fire.ElemAttackValue = 28;
        fire.ElemDefenseEnabled = 29;
        fire.ElemDefenseValue = 30;
        fire.StatusAttackValue = 31;
        fire.StatusDefenseValue = 32;
        fire.StatusAttackEnabled = 33;
        fire.StatusDefenseEnabled = 34;
        fire.QuezacoltCompatibility = 35;
        fire.ShivaCompatibility = 36;
        fire.IfritCompatibility = 37;
        fire.SirenCompatibility = 38;
        fire.BrothersCompatibility = 39;
        fire.DiablosCompatibility = 40;
        fire.CarbuncleCompatibility = 41;
        fire.LeviathanCompatibility = 42;
        fire.PandemonaCompatibility = 43;
        fire.CerberusCompatibility = 44;
        fire.AlexanderCompatibility = 45;
        fire.DoomtrainCompatibility = 46;
        fire.BahamutCompatibility = 47;
        fire.CactuarCompatibility = 48;
        fire.TonberryCompatibility = 49;
        fire.EdenCompatibility = 50;
        fire.Unknown6 = 51;
        data.MagicSpells.Add(fire);

        Guardian quezacotl = new Guardian();
        quezacotl.AttackName = "テスト０３";
        quezacotl.AttackDescription = "セツメイ０３";
        quezacotl.MagicId = 100;
        quezacotl.AttackType = 1;
        quezacotl.Power = 20;
        quezacotl.Flags = 2;
        quezacotl.AttackElement = Element.Thunder;
        quezacotl.AttackFlags = 3;
        quezacotl.Unknown2 = 4;
        quezacotl.Unknown3 = 5;
        quezacotl.SecondaryElement = Element.Wind;
        quezacotl.Statuses0 = 6;
        quezacotl.Statuses1 = 7;
        quezacotl.HpModifier = 8;
        quezacotl.Unknown4 = 9;
        quezacotl.Unknown5 = 10;
        quezacotl.Unknown6 = 11;
        quezacotl.ExpPerLevel = 12;
        quezacotl.Unknown7 = 13;
        quezacotl.Unknown8 = 14;
        quezacotl.StatusAttack = 15;
        for (Int32 i = 0; i < quezacotl.Abilities.Length; i++)
            quezacotl.Abilities[i] = i;
        for (Int32 i = 0; i < quezacotl.MagicCompatibility.Length; i++)
            quezacotl.MagicCompatibility[i] = (Byte)(i * 2);
        quezacotl.Unknown9 = 16;
        quezacotl.Unknown10 = 17;
        quezacotl.PowerModifier = 18;
        quezacotl.LevelModifier = 19;
        data.Guardians.Add(quezacotl);

        EnemyAttack enemyAttack = new EnemyAttack();
        enemyAttack.Name = "テスト０４";
        enemyAttack.MagicId = 1;
        enemyAttack.CameraChange = 2;
        enemyAttack.Unknown0 = 3;
        enemyAttack.AttackType = 4;
        enemyAttack.AttackPower = 5;
        enemyAttack.AttackFlags = 6;
        enemyAttack.Unknown1 = 7;
        enemyAttack.Element = Element.Ice;
        enemyAttack.Unknown2 = 8;
        enemyAttack.StatusAttack = 9;
        enemyAttack.AttackParam = 10;
        enemyAttack.Statuses0 = 11;
        enemyAttack.Statuses1 = 12;
        data.EnemyAttacks.Add(enemyAttack);

        Weapon weapon = new Weapon();
        weapon.Name = "テスト０５";
        weapon.RenzokukenFinishers = 1;
        weapon.CharacterId = 2;
        weapon.AttackType = 3;
        weapon.AttackPower = 4;
        weapon.AttackParam = 5;
        weapon.StrBonus = 6;
        weapon.Tier = 7;
        weapon.CritBonus = 8;
        weapon.Melee = 9;
        data.Weapons.Add(weapon);

        SquallLimit squallLimit = new SquallLimit();
        squallLimit.Name = "テスト０６";
        squallLimit.Description = "セツメイ０４";
        squallLimit.MagicId = 1;
        squallLimit.AttackType = 2;
        squallLimit.Unknown0 = 3;
        squallLimit.AttackPower = 4;
        squallLimit.Unknown1 = 5;
        squallLimit.Target = 6;
        squallLimit.AttackFlags = 7;
        squallLimit.HitCount = 8;
        squallLimit.Element = Element.None;
        squallLimit.ElementPercent = 9;
        squallLimit.StatusAttack = 10;
        squallLimit.Unknown2 = 11;
        squallLimit.Statuses0 = 12;
        squallLimit.Statuses1 = 13;
        data.SquallLimits.Add(squallLimit);

        Character squall = new Character();
        squall.Name = "テスト０７";
        squall.CrisisLevel = 1;
        squall.Gender = 2;
        squall.LimitId = 3;
        squall.LimitParam = 4;
        squall.Exp1 = 5;
        squall.Exp2 = 6;
        squall.Hp1 = 7;
        squall.Hp2 = 8;
        squall.Hp3 = 9;
        squall.Hp4 = 10;
        squall.Str1 = 11;
        squall.Str2 = 12;
        squall.Str3 = 13;
        squall.Str4 = 14;
        squall.Vit1 = 15;
        squall.Vit2 = 16;
        squall.Vit3 = 17;
        squall.Vit4 = 18;
        squall.Mag1 = 19;
        squall.Mag2 = 20;
        squall.Mag3 = 21;
        squall.Mag4 = 22;
        squall.Spr1 = 23;
        squall.Spr2 = 24;
        squall.Spr3 = 25;
        squall.Spr4 = 26;
        squall.Spd1 = 27;
        squall.Spd2 = 28;
        squall.Spd3 = 29;
        squall.Spd4 = 30;
        squall.Luck1 = 31;
        squall.Luck2 = 32;
        squall.Luck3 = 33;
        squall.Luck4 = 34;
        data.Characters.Add(squall);

        BattleItem potion = new BattleItem();
        potion.Name = "テスト０８";
        potion.Description = "セツメイ０５";
        potion.MagicId = 1;
        potion.AttackType = 2;
        potion.AttackPower = 3;
        potion.Unknown0 = 4;
        potion.Target = 5;
        potion.Unknown1 = 6;
        potion.AttackFlags = 7;
        potion.Unknown2 = 8;
        potion.StatusAttack = 9;
        potion.Statuses0 = 10;
        potion.Statuses1 = 11;
        potion.AttackParam = 12;
        potion.Unknown3 = 13;
        potion.HitCount = 14;
        potion.Element = Element.None;
        data.BattleItems.Add(potion);

        FieldItem fieldItem = new FieldItem();
        fieldItem.Name = "テスト０９";
        fieldItem.Description = "セツメイ０６";
        data.FieldItems.Add(fieldItem);

        IndependentGuardianAttack independentAttack = new IndependentGuardianAttack();
        independentAttack.AttackName = "テスト１０";
        independentAttack.MagicId = 1;
        independentAttack.AttackType = 2;
        independentAttack.Power = 3;
        independentAttack.Status = 4;
        independentAttack.Unknown0 = 5;
        independentAttack.Flags = 6;
        independentAttack.Unknown1 = 7;
        independentAttack.Element = Element.Earth;
        independentAttack.Statuses1 = 8;
        independentAttack.Statuses0 = 9;
        independentAttack.PowerModifier = 10;
        independentAttack.LevelModifier = 11;
        data.IndependentGuardianAttacks.Add(independentAttack);

        AdditionalCommand additionalCommand = new AdditionalCommand();
        additionalCommand.MagicId = 1;
        additionalCommand.Unknown = 2;
        additionalCommand.AttackType = 3;
        additionalCommand.AttackPower = 4;
        additionalCommand.AttackFlags = 5;
        additionalCommand.HitCount = 6;
        additionalCommand.Element = Element.Poison;
        additionalCommand.StatusAttack = 7;
        additionalCommand.Status1 = 8;
        additionalCommand.Status2 = 9;
        data.AdditionalCommands.Add(additionalCommand);

        JunctionAbility junctionAbility = new JunctionAbility();
        junctionAbility.Name = "テスト１１";
        junctionAbility.Description = "セツメイ０７";
        junctionAbility.AbilityPoints = 1;
        junctionAbility.Flag1 = 2;
        junctionAbility.Flag2 = 3;
        junctionAbility.Flag3 = 4;
        data.JunctionAbilities.Add(junctionAbility);

        CommandAbility commandAbility = new CommandAbility();
        commandAbility.Name = "テスト１２";
        commandAbility.Description = "セツメイ０８";
        commandAbility.AbilityPoints = 1;
        commandAbility.Index = 2;
        commandAbility.Unknown0 = 3;
        data.CommandAbilities.Add(commandAbility);

        CharacterStatAbility characterStatAbility = new CharacterStatAbility();
        characterStatAbility.Name = "テスト１３";
        characterStatAbility.Description = "セツメイ０９";
        characterStatAbility.AbilityPoints = 1;
        characterStatAbility.Stat = 2;
        characterStatAbility.Value = 3;
        characterStatAbility.Unknown0 = 4;
        data.CharacterStatAbilities.Add(characterStatAbility);

        CharacterAbility characterAbility = new CharacterAbility();
        characterAbility.Name = "テスト１４";
        characterAbility.Description = "セツメイ１０";
        characterAbility.AbilityPoints = 1;
        characterAbility.Flag1 = 2;
        characterAbility.Flag2 = 3;
        characterAbility.Flag3 = 4;
        data.CharacterAbilities.Add(characterAbility);

        PartyAbility partyAbility = new PartyAbility();
        partyAbility.Name = "テスト１５";
        partyAbility.Description = "セツメイ１１";
        partyAbility.AbilityPoints = 1;
        partyAbility.Flag1 = 2;
        partyAbility.Flag2 = 3;
        data.PartyAbilities.Add(partyAbility);

        GuardianAbility guardianAbility = new GuardianAbility();
        guardianAbility.Name = "テスト１６";
        guardianAbility.Description = "セツメイ１２";
        guardianAbility.AbilityPoints = 1;
        guardianAbility.Boost = 2;
        guardianAbility.Stat = 3;
        guardianAbility.Value = 4;
        data.GuardianAbilities.Add(guardianAbility);

        MenuAbility menuAbility = new MenuAbility();
        menuAbility.Name = "テスト１７";
        menuAbility.Description = "セツメイ１３";
        menuAbility.AbilityPoints = 1;
        menuAbility.Index = 2;
        menuAbility.Start = 3;
        menuAbility.End = 4;
        data.MenuAbilities.Add(menuAbility);

        NpcLimit npcLimit = new NpcLimit();
        npcLimit.Name = "テスト１８";
        npcLimit.Description = "セツメイ１４";
        npcLimit.MagicId = 1;
        npcLimit.AttackType = 2;
        npcLimit.AttackPower = 3;
        npcLimit.Unknown0 = 4;
        npcLimit.Target = 5;
        npcLimit.AttackFlags = 6;
        npcLimit.HitCount = 7;
        npcLimit.Element = Element.Fire;
        npcLimit.ElementPercent = 8;
        npcLimit.StatusAttack = 9;
        npcLimit.Statuses0 = 10;
        npcLimit.Unknown1 = 11;
        npcLimit.Statuses1 = 12;
        data.NpcLimits.Add(npcLimit);

        BlueMagic blueMagic = new BlueMagic();
        blueMagic.Name = "テスト１９";
        blueMagic.Description = "セツメイ１５";
        blueMagic.MagicId = 1;
        blueMagic.Unknown0 = 2;
        blueMagic.AttackType = 3;
        blueMagic.Unknown1 = 4;
        blueMagic.Target = 5;
        blueMagic.AttackFlags = 6;
        blueMagic.Unknown2 = 7;
        blueMagic.Element = 8;
        blueMagic.StatusAttack = 9;
        blueMagic.Crit = 10;
        blueMagic.Unknown3 = 11;
        data.BlueMagics.Add(blueMagic);

        QuistisLimit quistisLimit = new QuistisLimit();
        quistisLimit.Statuses1 = 1;
        quistisLimit.Statuses0 = 2;
        quistisLimit.AttackPower = 3;
        quistisLimit.AttackParam = 4;
        data.QuistisLimits.Add(quistisLimit);

        IrvinLimit irvinLimit = new IrvinLimit();
        irvinLimit.Name = "テスト２０";
        irvinLimit.Description = "セツメイ１６";
        irvinLimit.MagicId = 1;
        irvinLimit.AttackType = 2;
        irvinLimit.AttackPower = 3;
        irvinLimit.Unknown0 = 4;
        irvinLimit.Target = 5;
        irvinLimit.AttackFlags = 6;
        irvinLimit.HitCount = 7;
        irvinLimit.Element = Element.None;
        irvinLimit.ElementPercent = 8;
        irvinLimit.StatusAttack = 9;
        irvinLimit.Statuses0 = 10;
        irvinLimit.ItemIndex = 11;
        irvinLimit.Crit = 12;
        irvinLimit.Statuses1 = 13;
        data.IrvinLimits.Add(irvinLimit);

        ZellLimit zellLimit = new ZellLimit();
        zellLimit.Name = "テスト２１";
        zellLimit.Description = "セツメイ１７";
        zellLimit.MagicId = 1;
        zellLimit.AttackType = 2;
        zellLimit.AttackPower = 3;
        zellLimit.AttackFlags = 4;
        zellLimit.Unknown0 = 5;
        zellLimit.Target = 6;
        zellLimit.Unknown1 = 7;
        zellLimit.HitCount = 8;
        zellLimit.Element = Element.None;
        zellLimit.ElementPercent = 9;
        zellLimit.StatusAttack = 10;
        zellLimit.Combo1 = 11;
        zellLimit.Combo2 = 12;
        zellLimit.Combo3 = 13;
        zellLimit.Combo4 = 14;
        zellLimit.Combo5 = 15;
        zellLimit.Status0 = 16;
        zellLimit.Status1 = 17;
        data.ZellLimits.Add(zellLimit);

        ZellDuelMove zellDuelMove = new ZellDuelMove();
        zellDuelMove.StartMove = 1;
        zellDuelMove.NextSequence1 = 2;
        zellDuelMove.NextSequence2 = 3;
        zellDuelMove.NextSequence3 = 4;
        data.ZellDuelMoves.Add(zellDuelMove);

        RinoaLimit rinoaLimit = new RinoaLimit();
        rinoaLimit.Name = "テスト２２";
        rinoaLimit.Description = "セツメイ１８";
        rinoaLimit.Unknown = 1;
        rinoaLimit.Target = 2;
        rinoaLimit.AbilityId = 3;
        rinoaLimit.Unknown1 = 4;
        data.RinoaLimits.Add(rinoaLimit);

        RinoaAngeloAttack rinoaAngeloAttack = new RinoaAngeloAttack();
        rinoaAngeloAttack.Name = "テスト２３";
        rinoaAngeloAttack.MagicId = 1;
        rinoaAngeloAttack.AttackType = 2;
        rinoaAngeloAttack.AttackPower = 3;
        rinoaAngeloAttack.AttackFlags = 4;
        rinoaAngeloAttack.Unknown0 = 5;
        rinoaAngeloAttack.Target = 6;
        rinoaAngeloAttack.Unknown1 = 7;
        rinoaAngeloAttack.HitCount = 8;
        rinoaAngeloAttack.Element = Element.None;
        rinoaAngeloAttack.ElementPercent = 9;
        rinoaAngeloAttack.StatusAttack = 10;
        rinoaAngeloAttack.Statuses0 = 11;
        rinoaAngeloAttack.Statuses1 = 12;
        data.RinoaAngeloAttacks.Add(rinoaAngeloAttack);

        data.SelphieSlotIds.Add(1);
        data.SelphieSlotIds.Add(2);
        data.SelphieSlotIds.Add(3);

        SelphieSlotSet slotSet = new SelphieSlotSet();
        for (Int32 i = 0; i < slotSet.Slots.Length; i++)
        {
            SelphieMagicCount count = new SelphieMagicCount();
            count.MagicId = (Byte)i;
            count.Count = (Byte)(i + 1);
            slotSet.Slots[i] = count;
        }

        data.SelphieSlotSets.Add(slotSet);

        DevourEffect devourEffect = new DevourEffect();
        devourEffect.Description = "セツメイ１９";
        devourEffect.Effect = 1;
        devourEffect.Quantity = 2;
        devourEffect.Statuses1 = 3;
        devourEffect.Statuses0 = 4;
        devourEffect.StatFlags = 5;
        devourEffect.Hp = 6;
        data.DevourEffects.Add(devourEffect);

        TimerSettings timers = data.TimerSettings;
        for (Int32 i = 0; i < timers.StatusTimers.Length; i++)
            timers.StatusTimers[i] = (Byte)i;
        timers.AtbSpeedMultiplier = 5;
        timers.DeadTimer = 6;
        for (Int32 i = 0; i < timers.StatusLimitEffects.Length; i++)
            timers.StatusLimitEffects[i] = (Byte)(i + 1);
        for (Int32 i = 0; i < timers.DuelTimersAndStartMoves.Length; i++)
            timers.DuelTimersAndStartMoves[i] = (Byte)(i + 2);
        for (Int32 i = 0; i < timers.ShotTimers.Length; i++)
            timers.ShotTimers[i] = (Byte)(i + 3);

        data.MiscTexts.Add("テスト２４");
        data.MiscTexts.Add(null);
        data.MiscTexts.Add("テスト２５");
        data.MiscTexts.Add(String.Empty);

        return data;
    }

    private static void AssertEqual(KernelData expected, KernelData actual)
    {
        Assert.Equal(expected.BattleCommands.Count, actual.BattleCommands.Count);
        for (Int32 i = 0; i < expected.BattleCommands.Count; i++)
        {
            BattleCommand a = expected.BattleCommands[i];
            BattleCommand b = actual.BattleCommands[i];
            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Description, b.Description);
            Assert.Equal(a.AbilityId, b.AbilityId);
            Assert.Equal(a.Target, b.Target);
            Assert.Equal(a.Unknown1, b.Unknown1);
            Assert.Equal(a.Unknown2, b.Unknown2);
        }

        Assert.Single(actual.MagicSpells);
        MagicSpell expectedMagic = expected.MagicSpells[0];
        MagicSpell actualMagic = actual.MagicSpells[0];
        Assert.Equal(expectedMagic.Name, actualMagic.Name);
        Assert.Equal(expectedMagic.Description, actualMagic.Description);
        Assert.Equal(expectedMagic.MagicId, actualMagic.MagicId);
        Assert.Equal(expectedMagic.Element, actualMagic.Element);
        Assert.Equal(expectedMagic.EdenCompatibility, actualMagic.EdenCompatibility);
        Assert.Equal(expectedMagic.QuezacoltCompatibility, actualMagic.QuezacoltCompatibility);
        Assert.Equal(expectedMagic.Unknown6, actualMagic.Unknown6);
        Assert.Equal(expectedMagic.StatusDefenseEnabled, actualMagic.StatusDefenseEnabled);

        Assert.Single(actual.Guardians);
        Guardian expectedGuardian = expected.Guardians[0];
        Guardian actualGuardian = actual.Guardians[0];
        Assert.Equal(expectedGuardian.AttackName, actualGuardian.AttackName);
        Assert.Equal(expectedGuardian.AttackDescription, actualGuardian.AttackDescription);
        Assert.Equal(expectedGuardian.AttackElement, actualGuardian.AttackElement);
        Assert.Equal(expectedGuardian.SecondaryElement, actualGuardian.SecondaryElement);
        Assert.Equal(expectedGuardian.Abilities, actualGuardian.Abilities);
        Assert.Equal(expectedGuardian.MagicCompatibility, actualGuardian.MagicCompatibility);
        Assert.Equal(expectedGuardian.LevelModifier, actualGuardian.LevelModifier);

        Assert.Single(actual.EnemyAttacks);
        Assert.Equal(expected.EnemyAttacks[0].Name, actual.EnemyAttacks[0].Name);
        Assert.Equal(expected.EnemyAttacks[0].Element, actual.EnemyAttacks[0].Element);
        Assert.Equal(expected.EnemyAttacks[0].Statuses1, actual.EnemyAttacks[0].Statuses1);

        Assert.Single(actual.Weapons);
        Assert.Equal(expected.Weapons[0].Name, actual.Weapons[0].Name);
        Assert.Equal(expected.Weapons[0].Melee, actual.Weapons[0].Melee);

        Assert.Single(actual.SquallLimits);
        Assert.Equal(expected.SquallLimits[0].Name, actual.SquallLimits[0].Name);
        Assert.Equal(expected.SquallLimits[0].Description, actual.SquallLimits[0].Description);
        Assert.Equal(expected.SquallLimits[0].Statuses1, actual.SquallLimits[0].Statuses1);

        Assert.Single(actual.Characters);
        Assert.Equal(expected.Characters[0].Name, actual.Characters[0].Name);
        Assert.Equal(expected.Characters[0].Luck4, actual.Characters[0].Luck4);

        Assert.Single(actual.BattleItems);
        Assert.Equal(expected.BattleItems[0].Name, actual.BattleItems[0].Name);
        Assert.Equal(expected.BattleItems[0].Description, actual.BattleItems[0].Description);
        Assert.Equal(expected.BattleItems[0].Statuses1, actual.BattleItems[0].Statuses1);

        Assert.Single(actual.FieldItems);
        Assert.Equal(expected.FieldItems[0].Name, actual.FieldItems[0].Name);
        Assert.Equal(expected.FieldItems[0].Description, actual.FieldItems[0].Description);

        Assert.Single(actual.IndependentGuardianAttacks);
        Assert.Equal(expected.IndependentGuardianAttacks[0].AttackName, actual.IndependentGuardianAttacks[0].AttackName);
        Assert.Equal(expected.IndependentGuardianAttacks[0].Element, actual.IndependentGuardianAttacks[0].Element);

        Assert.Single(actual.AdditionalCommands);
        Assert.Equal(expected.AdditionalCommands[0].Element, actual.AdditionalCommands[0].Element);
        Assert.Equal(expected.AdditionalCommands[0].Status2, actual.AdditionalCommands[0].Status2);

        Assert.Single(actual.JunctionAbilities);
        Assert.Equal(expected.JunctionAbilities[0].Name, actual.JunctionAbilities[0].Name);

        Assert.Single(actual.CommandAbilities);
        Assert.Equal(expected.CommandAbilities[0].Name, actual.CommandAbilities[0].Name);

        Assert.Single(actual.CharacterStatAbilities);
        Assert.Equal(expected.CharacterStatAbilities[0].Name, actual.CharacterStatAbilities[0].Name);

        Assert.Single(actual.CharacterAbilities);
        Assert.Equal(expected.CharacterAbilities[0].Name, actual.CharacterAbilities[0].Name);

        Assert.Single(actual.PartyAbilities);
        Assert.Equal(expected.PartyAbilities[0].Name, actual.PartyAbilities[0].Name);

        Assert.Single(actual.GuardianAbilities);
        Assert.Equal(expected.GuardianAbilities[0].Name, actual.GuardianAbilities[0].Name);

        Assert.Single(actual.MenuAbilities);
        Assert.Equal(expected.MenuAbilities[0].Name, actual.MenuAbilities[0].Name);

        Assert.Single(actual.NpcLimits);
        Assert.Equal(expected.NpcLimits[0].Name, actual.NpcLimits[0].Name);
        Assert.Equal(expected.NpcLimits[0].Element, actual.NpcLimits[0].Element);

        Assert.Single(actual.BlueMagics);
        Assert.Equal(expected.BlueMagics[0].Name, actual.BlueMagics[0].Name);
        Assert.Equal(expected.BlueMagics[0].Element, actual.BlueMagics[0].Element);

        Assert.Single(actual.QuistisLimits);
        Assert.Equal(expected.QuistisLimits[0].Statuses1, actual.QuistisLimits[0].Statuses1);
        Assert.Equal(expected.QuistisLimits[0].AttackParam, actual.QuistisLimits[0].AttackParam);

        Assert.Single(actual.IrvinLimits);
        Assert.Equal(expected.IrvinLimits[0].Name, actual.IrvinLimits[0].Name);
        Assert.Equal(expected.IrvinLimits[0].Element, actual.IrvinLimits[0].Element);

        Assert.Single(actual.ZellLimits);
        Assert.Equal(expected.ZellLimits[0].Name, actual.ZellLimits[0].Name);
        Assert.Equal(expected.ZellLimits[0].Combo5, actual.ZellLimits[0].Combo5);

        Assert.Single(actual.ZellDuelMoves);
        Assert.Equal(expected.ZellDuelMoves[0].StartMove, actual.ZellDuelMoves[0].StartMove);
        Assert.Equal(expected.ZellDuelMoves[0].NextSequence3, actual.ZellDuelMoves[0].NextSequence3);

        Assert.Single(actual.RinoaLimits);
        Assert.Equal(expected.RinoaLimits[0].Name, actual.RinoaLimits[0].Name);

        Assert.Single(actual.RinoaAngeloAttacks);
        Assert.Equal(expected.RinoaAngeloAttacks[0].Name, actual.RinoaAngeloAttacks[0].Name);
        Assert.Equal(expected.RinoaAngeloAttacks[0].Element, actual.RinoaAngeloAttacks[0].Element);

        Assert.Equal(expected.SelphieSlotIds, actual.SelphieSlotIds);

        Assert.Single(actual.SelphieSlotSets);
        for (Int32 i = 0; i < expected.SelphieSlotSets[0].Slots.Length; i++)
        {
            Assert.Equal(expected.SelphieSlotSets[0].Slots[i].MagicId, actual.SelphieSlotSets[0].Slots[i].MagicId);
            Assert.Equal(expected.SelphieSlotSets[0].Slots[i].Count, actual.SelphieSlotSets[0].Slots[i].Count);
        }

        Assert.Single(actual.DevourEffects);
        Assert.Equal(expected.DevourEffects[0].Description, actual.DevourEffects[0].Description);
        Assert.Equal(expected.DevourEffects[0].Statuses1, actual.DevourEffects[0].Statuses1);

        Assert.Equal(expected.TimerSettings.StatusTimers, actual.TimerSettings.StatusTimers);
        Assert.Equal(expected.TimerSettings.AtbSpeedMultiplier, actual.TimerSettings.AtbSpeedMultiplier);
        Assert.Equal(expected.TimerSettings.DeadTimer, actual.TimerSettings.DeadTimer);
        Assert.Equal(expected.TimerSettings.StatusLimitEffects, actual.TimerSettings.StatusLimitEffects);
        Assert.Equal(expected.TimerSettings.DuelTimersAndStartMoves, actual.TimerSettings.DuelTimersAndStartMoves);
        Assert.Equal(expected.TimerSettings.ShotTimers, actual.TimerSettings.ShotTimers);

        Assert.Equal(expected.MiscTexts, actual.MiscTexts);
    }
}
