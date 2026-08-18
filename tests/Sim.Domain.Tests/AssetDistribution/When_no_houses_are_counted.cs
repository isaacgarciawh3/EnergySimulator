using Shouldly;

namespace Sim.Domain.Tests.AssetDistributionScenario;

/// <summary>A distribution over nothing is zero, not a division error - the empty case answers calmly.</summary>
public class When_no_houses_are_counted
{
    private readonly Sim.Energy.Domain.AssetDistribution _distribution =
        Sim.Energy.Domain.AssetDistribution.Of([]);

    [Fact] public void Should_report_no_solar_share() => _distribution.PvShare.ShouldBe(0);
    [Fact] public void Should_report_no_houses() => _distribution.Houses.ShouldBe(0);
    [Fact] public void Should_still_describe_itself() => _distribution.ToString().ShouldContain("0 houses");
}
