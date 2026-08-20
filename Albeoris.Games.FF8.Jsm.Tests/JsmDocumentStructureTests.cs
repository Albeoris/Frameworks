using Albeoris.Games.FF8.Jsm.Instructions;
using Xunit;

namespace Albeoris.Games.FF8.Jsm.Tests;

public sealed class JsmDocumentStructureTests
{
    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Read_ExposesTheSameInstructionsThroughTheScriptHierarchy(JsmSample sample)
    {
        JsmDocument document = sample.Read();

        IReadOnlyList<IJsmInstruction> scriptInstructions = document.Objects
            .SelectMany(gameObject => gameObject.Scripts)
            .SelectMany(script => script.Instructions)
            .ToArray();

        Assert.Equal(document.Instructions, scriptInstructions);
    }

    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void Read_BindsMessageConstantsToTheirSourceOperations(JsmSample sample)
    {
        Jsm.Expression.PSHN_L[] constants = sample.Read().Instructions
            .OfType<IMessageInstruction>()
            .Cast<JsmInstruction>()
            .SelectMany(instruction => instruction.Operands)
            .OfType<Jsm.Expression.PSHN_L>()
            .ToArray();

        Assert.NotEmpty(constants);
        Assert.All(constants, constant => Assert.Equal(Jsm.Opcode.PSHN_L, constant.SourceOperation?.Opcode));
    }

    [Theory]
    [MemberData(nameof(JsmSample.All), MemberType = typeof(JsmSample))]
    public void GetInstructions_ReturnsAReadOnlyCollection(JsmSample sample)
    {
        IReadOnlyList<IJsmInstruction> instructions = Jsm.File.GetInstructions(sample.Content);

        IList<IJsmInstruction> collection = Assert.IsAssignableFrom<IList<IJsmInstruction>>(instructions);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(instructions[0]));
    }
}
