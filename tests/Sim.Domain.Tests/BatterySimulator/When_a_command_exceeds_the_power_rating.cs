using Shouldly;
using Sim.SharedKernel;
using static Sim.Domain.Tests.BatterySimulatorScenario.BatteryScenario;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>R-44 (max charge/discharge power): the hardware clamps; a Setpoint is a request, the PowerReading is the truth.</summary>
public class When_a_command_exceeds_the_power_rating
{
    private readonly PowerReading _reading = Fresh().Apply(Command(MaxPowerKw * 10), Instant, Hour);

    [Fact]
    public void Should_clamp_the_metered_power_to_the_rating() =>
        _reading.Power.Value.ShouldBeLessThanOrEqualTo(MaxPowerKw + AbsoluteTolerance);
}
