using Shouldly;
using Sim.SharedKernel;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>
/// TASK-016's rule says the rating is the law IN BOTH DIRECTIONS - and until
/// this scenario, only the charge direction was proven.
/// </summary>
public class When_a_discharge_command_exceeds_the_power_rating
{
    private readonly PowerReading _reading = Fresh().Apply(Command(-MaxPowerKw * 10), Instant, Hour);

    [Fact]
    public void Should_clamp_the_metered_power_to_the_rating() =>
        (-_reading.Power.Value).ShouldBeLessThanOrEqualTo(MaxPowerKw + AbsoluteTolerance);
}
