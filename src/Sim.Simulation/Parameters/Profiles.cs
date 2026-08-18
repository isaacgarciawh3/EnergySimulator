namespace Sim.Simulation.Parameters;

/// <summary>
/// The Simulation context's own view of the tunable parameters. It does not
/// take the application's options class directly, so the configuration file
/// format can change without touching a behaviour.
/// </summary>
public interface IDailyShape
{
    double At(double hour);
}

public sealed record HomeChargerProfile(
    double SessionMinKwh,
    double SessionMaxKwh,
    double PlugInFromHour,
    double PlugInToHour,
    double DepartureHour)
{
    public static readonly HomeChargerProfile Default = new(8.0, 12.0, 17.5, 19.0, 7.0);
}

public sealed record PublicChargerProfile(
    double SessionMinKwh,
    double SessionMaxKwh,
    IReadOnlyList<double> ArrivalsPerHourByBand,
    IReadOnlyList<double> BandUpperHours)
{
    public static readonly PublicChargerProfile Default =
        new(10.0, 40.0, [0.05, 0.20, 0.35, 0.45, 0.10], [6, 10, 15, 21, 24]);

    public double ArrivalsPerHour(double hour)
    {
        for (var i = 0; i < BandUpperHours.Count; i++)
            if (hour < BandUpperHours[i]) return ArrivalsPerHourByBand[i];
        return ArrivalsPerHourByBand[^1];
    }
}

/// <summary>Everything the simulator needs to build behaviours.</summary>
public sealed record SimulationProfiles(
    IDailyShape BaseLoadShape,
    double HeatPumpBalancePointC,
    HomeChargerProfile HomeCharger,
    PublicChargerProfile PublicCharger)
{
    public static readonly SimulationProfiles Default =
        new(new FlatDailyShape(), 15.0, HomeChargerProfile.Default, PublicChargerProfile.Default);
}

/// <summary>Fallback used when no configuration file is present.</summary>
public sealed class FlatDailyShape : IDailyShape
{
    public double At(double hour) => hour switch
    {
        < 6 => 0.55, < 9 => 1.5, < 17 => 0.9, < 22 => 1.8, _ => 0.8,
    };
}
