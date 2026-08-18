using Shouldly;
using Sim.Accounting.Domain;
using Sim.SharedKernel;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.EnergyLedgerScenario;

/// <summary>R-10: energy scales with the interval length, never with how many posts sliced it.</summary>
public class When_one_hour_is_posted_in_quarters_and_whole
{
    private readonly EnergyLedger _quarterly = new();
    private readonly EnergyLedger _hourly = new();

    public When_one_hour_is_posted_in_quarters_and_whole()
    {
        for (var i = 0; i < 4; i++)
            _quarterly.Post(Instant + i * Quarter, Quarter, [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);
        _hourly.Post(Instant, TimeSpan.FromHours(1), [new PowerReading("kettle", Instant, new Kilowatts(4.0))]);
    }

    [Fact]
    public void Should_reach_the_same_total_either_way() =>
        _quarterly.TotalConsumed.Value.ShouldBe(_hourly.TotalConsumed.Value, Close(4.0, 4.0));

    [Fact] public void Should_total_four_kilowatt_hours() => _hourly.TotalConsumed.Value.ShouldBe(4.0, AbsoluteTolerance);
}
