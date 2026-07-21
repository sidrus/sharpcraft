using AwesomeAssertions;
using SharpCraft.Client.Gameplay;
using System.Numerics;

namespace SharpCraft.Client.Tests.Gameplay;

public class TorchPlacementTests
{
    [Fact]
    public void CanPlace_OnSolidGroundAndNotInFluid_ShouldBeTrue()
    {
        TorchPlacement.CanPlace(blockBelowSolid: true, isSwimming: false, isUnderwater: false)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false, false, false)] // no solid block below
    [InlineData(true, true, false)]   // swimming
    [InlineData(true, false, true)]   // underwater
    public void CanPlace_WhenUnsupportedOrInFluid_ShouldBeFalse(bool solid, bool swimming, bool underwater)
    {
        TorchPlacement.CanPlace(solid, swimming, underwater).Should().BeFalse();
    }

    [Fact]
    public void BasePosition_ShouldCenterOnBlockColumnTop()
    {
        var basePos = TorchPlacement.BasePosition(new Vector3(4.7f, 65.9f, -3.2f));

        basePos.Should().Be(new Vector3(4.5f, 65f, -3.5f));
    }
}