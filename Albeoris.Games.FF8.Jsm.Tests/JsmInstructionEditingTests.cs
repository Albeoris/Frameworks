using System.Buffers.Binary;
using Albeoris.Games.FF8.Jsm.Instructions;
using Xunit;

namespace Albeoris.Games.FF8.Jsm.Tests;

public sealed class JsmInstructionEditingTests
{
    private const Int32 Replacement = -1_777;

    [Fact]
    public void Read_RecognizesEveryMessageInstructionTypeInTheSamples()
    {
        Type[] messageInstructionTypes = InstructionsWithMessageIdProperty()
            .Select(instruction => instruction.GetType())
            .Distinct()
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.Equal(
            [typeof(AASK), typeof(AMES), typeof(AMESW), typeof(MES), typeof(RAMESW)],
            messageInstructionTypes);
    }

    [Fact]
    public void Read_RepresentsEveryMessageIdPropertyThroughIMessageInstruction()
    {
        IJsmInstruction[] instructions = InstructionsWithMessageIdProperty();

        Assert.All(instructions, instruction => Assert.IsAssignableFrom<IMessageInstruction>(instruction));
    }

    [Fact]
    public void Ames_SettersUpdateItsConstantOperands()
    {
        AMES instruction = JsmSample.MessageDialogs.Read().Instructions.OfType<AMES>().First();

        instruction.Channel = -101;
        instruction.MessageId = -102;
        instruction.PosX = -103;
        instruction.PosY = -104;

        Assert.Equal([-101, -102, -103, -104], ConstantValues(instruction));
    }

    [Fact]
    public void Mes_SettersUpdateItsConstantOperands()
    {
        MES instruction = JsmSample.FieldMessage.Read().Instructions.OfType<MES>().First();

        instruction.Channel = -101;
        instruction.MessageId = -102;

        Assert.Equal([-101, -102], ConstantValues(instruction));
    }

    [Fact]
    public void PshnValue_UpdatesItsSourceOperation()
    {
        Jsm.Expression.PSHN_L messageId = GetFirstMessageId();

        messageId.Value = Replacement;

        Assert.Equal(Replacement, messageId.SourceOperation?.Parameter);
    }

    [Fact]
    public void SourceOperationParameter_UpdatesItsBoundExpression()
    {
        Jsm.Expression.PSHN_L messageId = GetFirstMessageId();
        JsmOperation sourceOperation = Assert.IsType<JsmOperation>(messageId.SourceOperation);

        sourceOperation.Parameter = Replacement;

        Assert.Equal(Replacement, messageId.Value);
    }

    [Fact]
    public void Write_ChangesOnlyTheEditedMessageIdOperand()
    {
        JsmDocument document = JsmSample.MessageDialogs.Read();
        AMES instruction = document.Instructions.OfType<AMES>().First();
        Jsm.Expression.PSHN_L messageId = Assert.IsType<Jsm.Expression.PSHN_L>(
            ((IMessageInstruction)instruction).MessageIdExpression);
        JsmOperation sourceOperation = Assert.IsType<JsmOperation>(messageId.SourceOperation);
        Byte[] expected = ReplaceParameter(JsmSample.MessageDialogs.Content, sourceOperation.Offset, Replacement);

        instruction.MessageId = Replacement;

        Assert.Equal(expected, document.Write());
    }

    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Write_PreservesEditedMessageConstantsAfterReload(JsmSample sample)
    {
        (_, JsmInstruction[] reloadedInstructions) = EditMessageConstantsAndReload(sample);

        Assert.All(
            reloadedInstructions.SelectMany(instruction => instruction.Operands).OfType<Jsm.Expression.PSHN_L>(),
            constant => Assert.Equal(Replacement, constant.Value));
    }

    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Write_PreservesMessageInstructionTypesAfterReload(JsmSample sample)
    {
        (JsmInstruction[] original, JsmInstruction[] reloaded) = EditMessageConstantsAndReload(sample);

        Assert.Equal(
            original.Select(instruction => instruction.GetType()),
            reloaded.Select(instruction => instruction.GetType()));
    }

    [Theory]
    [InlineData(JsmOperation.MinimumParameter - 1)]
    [InlineData(JsmOperation.MaximumParameter + 1)]
    public void PshnValue_RejectsValuesOutsideTheEncodedRange(Int32 value)
    {
        Jsm.Expression.PSHN_L constant = new(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => constant.Value = value);
    }

    private static Jsm.Expression.PSHN_L GetFirstMessageId()
    {
        IMessageInstruction instruction = JsmSample.MessageDialogs.Read().Instructions
            .OfType<IMessageInstruction>()
            .First();
        return Assert.IsType<Jsm.Expression.PSHN_L>(instruction.MessageIdExpression);
    }

    private static IJsmInstruction[] InstructionsWithMessageIdProperty()
    {
        return JsmSample.Values
            .SelectMany(sample => sample.Read().Instructions)
            .Where(instruction => instruction.GetType().GetProperty(nameof(AMES.MessageId)) is not null)
            .ToArray();
    }

    private static (JsmInstruction[] Original, JsmInstruction[] Reloaded) EditMessageConstantsAndReload(
        JsmSample sample)
    {
        JsmDocument document = sample.Read();
        JsmInstruction[] original = document.Instructions
            .OfType<IMessageInstruction>()
            .Cast<JsmInstruction>()
            .ToArray();

        foreach (Jsm.Expression.PSHN_L constant in original
                     .SelectMany(instruction => instruction.Operands)
                     .OfType<Jsm.Expression.PSHN_L>())
        {
            constant.Value = Replacement;
        }

        JsmInstruction[] reloaded = Jsm.File.ReadDocument(document.Write()).Instructions
            .OfType<IMessageInstruction>()
            .Cast<JsmInstruction>()
            .ToArray();
        return (original, reloaded);
    }

    private static Int32[] ConstantValues(JsmInstruction instruction)
    {
        return instruction.Operands
            .Cast<Jsm.Expression.PSHN_L>()
            .Select(constant => constant.Value)
            .ToArray();
    }

    private static Byte[] ReplaceParameter(Byte[] source, Int32 offset, Int32 value)
    {
        Byte[] expected = (Byte[])source.Clone();
        Span<Byte> operation = expected.AsSpan(offset, sizeof(Int32));
        Int32 encodedValue = BinaryPrimitives.ReadInt32LittleEndian(operation);
        encodedValue = (encodedValue & unchecked((Int32)0xFF00_0000)) | (value & 0x00FF_FFFF);
        BinaryPrimitives.WriteInt32LittleEndian(operation, encodedValue);
        return expected;
    }
}
