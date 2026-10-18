using Albeoris.Games.FF8.TextEncoding.Tags;
using Xunit;

namespace Albeoris.Games.FF8.TextEncoding.Tests;

public sealed class KeyIconTextEncodingTests
{
    [Theory]
    [InlineData(0x41, nameof(TextTagKey.JunctionMarker))]
    [InlineData(0x45, nameof(TextTagKey.JunctionAbility))]
    [InlineData(0x46, nameof(TextTagKey.CommandAbility))]
    [InlineData(0x47, nameof(TextTagKey.ActionAbility))]
    [InlineData(0x48, nameof(TextTagKey.CharacterAbility))]
    [InlineData(0x49, nameof(TextTagKey.PartyAbility))]
    [InlineData(0x4A, nameof(TextTagKey.GuardianForceAbility))]
    [InlineData(0x4B, nameof(TextTagKey.MenuAbility))]
    [InlineData(0x5D, nameof(TextTagKey.FireElement))]
    [InlineData(0x5E, nameof(TextTagKey.IceElement))]
    [InlineData(0x5F, nameof(TextTagKey.ThunderElement))]
    [InlineData(0x60, nameof(TextTagKey.EarthElement))]
    [InlineData(0x61, nameof(TextTagKey.PoisonElement))]
    [InlineData(0x62, nameof(TextTagKey.WindElement))]
    [InlineData(0x63, nameof(TextTagKey.WaterElement))]
    [InlineData(0x64, nameof(TextTagKey.HolyElement))]
    public void InlineIcon_DecodesToSemanticNameAndRoundTrips(Int32 parameter, String name)
    {
        FF8Encoding encoding = FF8Encoding.CreateEuropean();
        Byte[] bytes = [(Byte)TextTagCode.Key, (Byte)parameter];

        String text = encoding.GetString(bytes);

        Assert.Equal($"{{Key {name}}}", text);
        Assert.Equal(bytes, encoding.GetBytes(text));
    }

    [Fact]
    public void UnnamedKey_PreservesNumericParameter()
    {
        const Byte parameter = 0xFF;
        FF8Encoding encoding = FF8Encoding.CreateEuropean();
        Byte[] bytes = [(Byte)TextTagCode.Key, (Byte)parameter];

        String text = encoding.GetString(bytes);

        Assert.Equal($"{{Key {parameter}}}", text);
        Assert.Equal(bytes, encoding.GetBytes(text));
    }
}
