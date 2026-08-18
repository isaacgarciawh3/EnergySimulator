using Shouldly;
using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests;

/// <summary>
/// The battery simulator is the only place in the system where a command becomes
/// a physical consequence. Two things must hold no matter what it is told to do:
/// it cannot store energy it does not have room for, and it cannot hand back more
/// than it took in. Everything else about the dashboard is cosmetic by comparison.
/// </summary>
public sealed class BatteryPhysicsTests
{
    private const double CapacityKwh = 100;
    private const double MaxPowerKw = 50;
    private const double RoundTrip = 0.9;

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);

    private static BatterySimulator Fresh(double roundTrip = RoundTrip) =>
        new(new Battery("neighbourhood/battery", CapacityKwh, MaxPowerKw, roundTrip));

    private static StorageSetpoint Command(double kw) => new(new Kilowatts(kw));

    // 16
    [Fact]
    public void State_of_charge_stays_within_the_battery_across_a_long_command_sequence()
    {
        const ulong seed = 20260818;
        var battery = Fresh();
        var instant = Instant;

        for (var i = 0; i < 5_000; i++)
        {
            // Deliberately hostile: commands up to four times the power rating, both directions.
            var commanded = -200.0 + 400.0 * DeterministicNoise.Sample(seed, 3, i);
            var duration = TimeSpan.FromMinutes(1 + 59 * DeterministicNoise.Sample(seed, 4, i));

            battery.Apply(Command(commanded), instant, duration);
            instant += duration;

            battery.StateOfChargeKwh.ShouldBeGreaterThanOrEqualTo(-AbsoluteTolerance);
            battery.StateOfChargeKwh.ShouldBeLessThanOrEqualTo(CapacityKwh + AbsoluteTolerance);
            battery.StateOfChargePercent.ShouldBeInRange(-AbsoluteTolerance, 100 + AbsoluteTolerance);
        }
    }

    // 16
    [Fact]
    public void A_battery_starts_half_full_so_the_first_peak_has_something_to_shave()
    {
        var battery = Fresh();

        battery.StateOfChargeKwh.ShouldBe(CapacityKwh / 2, AbsoluteTolerance);
        battery.CapacityKwh.ShouldBe(CapacityKwh, AbsoluteTolerance);
        battery.StateOfChargePercent.ShouldBe(50, AbsoluteTolerance);
    }

    // 17
    [Fact]
    public void A_full_round_trip_returns_less_energy_than_it_took_in()
    {
        var battery = Fresh();

        DrainCompletely(battery);
        var energyInKwh = FillCompletely(battery);
        var energyOutKwh = DrainCompletely(battery);

        energyOutKwh.ShouldBeLessThan(energyInKwh);

        // Empty to full to empty again: the losses are exactly the round trip efficiency.
        var observed = energyOutKwh / energyInKwh;
        observed.ShouldBe(RoundTrip, Close(observed, RoundTrip));
    }

    // 17
    [Fact]
    public void A_lossless_battery_returns_everything_it_took_in()
    {
        var battery = Fresh(roundTrip: 1.0);

        DrainCompletely(battery);
        var energyInKwh = FillCompletely(battery);
        var energyOutKwh = DrainCompletely(battery);

        energyOutKwh.ShouldBe(energyInKwh, Close(energyInKwh, energyOutKwh));
    }

    // 18
    [Fact]
    public void Charging_is_reported_as_positive_power_at_the_meter()
    {
        var battery = Fresh();

        var reading = battery.Apply(Command(30), Instant, Hour);

        reading.MeterId.ShouldBe("neighbourhood/battery");
        reading.Instant.ShouldBe(Instant);
        reading.Power.Value.ShouldBeGreaterThan(0);
        battery.StateOfChargeKwh.ShouldBeGreaterThan(CapacityKwh / 2);
    }

    // 18
    [Fact]
    public void Discharging_is_reported_as_negative_power_at_the_meter()
    {
        var battery = Fresh();

        var reading = battery.Apply(Command(-30), Instant, Hour);

        reading.Power.Value.ShouldBeLessThan(0);
        battery.StateOfChargeKwh.ShouldBeLessThan(CapacityKwh / 2);
    }

    // 18
    [Fact]
    public void An_idle_command_moves_no_energy_and_meters_nothing()
    {
        var battery = Fresh();

        var reading = battery.Apply(StorageSetpoint.Idle, Instant, Hour);

        reading.Power.Value.ShouldBe(0, AbsoluteTolerance);
        battery.StateOfChargeKwh.ShouldBe(CapacityKwh / 2, AbsoluteTolerance);
    }

    // 18
    [Fact]
    public void A_command_beyond_the_power_rating_is_clamped_to_the_rating()
    {
        var battery = Fresh();

        var reading = battery.Apply(Command(MaxPowerKw * 10), Instant, Hour);

        reading.Power.Value.ShouldBeLessThanOrEqualTo(MaxPowerKw + AbsoluteTolerance);
    }

    /// <summary>Commands full discharge until the cells are empty. Returns metered energy delivered, in kWh.</summary>
    private static double DrainCompletely(BatterySimulator battery)
    {
        var deliveredKwh = 0.0;
        for (var i = 0; i < 40; i++)
            deliveredKwh += -battery.Apply(Command(-MaxPowerKw), Instant, Hour).Power.Value;

        battery.StateOfChargeKwh.ShouldBe(0, AbsoluteTolerance);
        return deliveredKwh;
    }

    /// <summary>Commands full charge until the cells are full. Returns metered energy absorbed, in kWh.</summary>
    private static double FillCompletely(BatterySimulator battery)
    {
        var absorbedKwh = 0.0;
        for (var i = 0; i < 40; i++)
            absorbedKwh += battery.Apply(Command(MaxPowerKw), Instant, Hour).Power.Value;

        battery.StateOfChargeKwh.ShouldBe(CapacityKwh, AbsoluteTolerance);
        return absorbedKwh;
    }
}
