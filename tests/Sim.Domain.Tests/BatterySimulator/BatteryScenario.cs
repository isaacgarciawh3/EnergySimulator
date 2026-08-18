using Sim.Control.Domain;
using Sim.Energy.Domain;
using Sim.SharedKernel;
using Sim.Simulation.Domain;
using static Sim.Domain.Tests.TestWorld;

namespace Sim.Domain.Tests.BatterySimulatorScenario;

/// <summary>Shared vocabulary for the battery physics scenarios.</summary>
internal static class BatteryScenario
{
    public const double CapacityKwh = 100;
    public const double MaxPowerKw = 50;
    public const double RoundTrip = 0.9;
    public static readonly TimeSpan Hour = TimeSpan.FromHours(1);

    public static BatterySimulator Fresh(double roundTrip = RoundTrip) =>
        new(new Battery("neighbourhood/battery", CapacityKwh, MaxPowerKw, roundTrip));

    public static StorageSetpoint Command(double kw) => new(new Kilowatts(kw));

    /// <summary>Commands full discharge until the cells are empty. Returns metered energy delivered, in kWh.</summary>
    public static double DrainCompletely(BatterySimulator battery)
    {
        var deliveredKwh = 0.0;
        for (var i = 0; i < 40; i++)
            deliveredKwh += -battery.Apply(Command(-MaxPowerKw), Instant, Hour).Power.Value;
        return deliveredKwh;
    }

    /// <summary>Commands full charge until the cells are full. Returns metered energy absorbed, in kWh.</summary>
    public static double FillCompletely(BatterySimulator battery)
    {
        var absorbedKwh = 0.0;
        for (var i = 0; i < 40; i++)
            absorbedKwh += battery.Apply(Command(MaxPowerKw), Instant, Hour).Power.Value;
        return absorbedKwh;
    }
}
