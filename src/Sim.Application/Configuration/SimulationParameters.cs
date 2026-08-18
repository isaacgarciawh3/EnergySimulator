using Sim.Simulation.Domain.Weather;
using Sim.Simulation.Parameters;

namespace Sim.Application.Configuration;

/// <summary>
/// The physical parameters of the simulated world, bound from
/// appsettings.Simulation.json at startup.
///
/// These used to be magic numbers inside the builder and the behaviours, which
/// meant changing the scenario required editing and recompiling C#, and meant a
/// reader could not see the scenario without reading the code.
///
/// What is deliberately NOT here: the house count and the public charger count.
/// The assignment states "exactly 30 houses" and "exactly 6 public chargers".
/// Those are constraints, not settings - if a configuration file could change
/// them, the file could violate a stated requirement. They stay as constants
/// enforced in the Neighbourhood constructor.
/// </summary>
public sealed class SimulationParameters
{
    public const string SectionName = "Simulation";

    public RangeKw BaseLoadKw { get; init; } = new(0.2, 0.6);
    public RangeKw PvCapacityKwp { get; init; } = new(3.0, 8.0);
    public HeatPumpParameters HeatPump { get; init; } = new();
    public HomeChargerParameters HomeCharger { get; init; } = new();
    public PublicChargerParameters PublicCharger { get; init; } = new();

    /// <summary>Multipliers applied to the household baseline through the day.</summary>
    public DailyShape BaseLoadShape { get; init; } = new();

    /// <summary>Climate constants. See WeatherParameters for what each one means.</summary>
    public WeatherSettings Weather { get; init; } = new();

    /// <summary>Translates the file format into what the Simulation context asks for.</summary>
    public SimulationProfiles ToProfiles() => new(
        new ConfiguredDailyShape(BaseLoadShape),
        HeatPump.BalancePointC,
        new HomeChargerProfile(HomeCharger.SessionKwh.Min, HomeCharger.SessionKwh.Max,
            HomeCharger.PlugInFromHour, HomeCharger.PlugInToHour, HomeCharger.DepartureHour),
        new PublicChargerProfile(PublicCharger.SessionKwh.Min, PublicCharger.SessionKwh.Max,
            PublicCharger.ArrivalsPerHourByBand, PublicCharger.BandUpperHours),
        Weather.ToParameters());

    public void Validate()
    {
        BaseLoadKw.Validate(nameof(BaseLoadKw));
        PvCapacityKwp.Validate(nameof(PvCapacityKwp));
        HeatPump.Validate();
        HomeCharger.Validate();
        PublicCharger.Validate();
        Weather.ToParameters().Validate();
    }
}

public sealed record RangeKw(double Min, double Max)
{
    public double Spread => Max - Min;

    public void Validate(string name)
    {
        if (Min < 0 || Max < Min)
            throw new InvalidOperationException($"Simulation parameter '{name}' is invalid: Min={Min}, Max={Max}.");
    }
}

public sealed class HeatPumpParameters
{
    /// <summary>Outdoor temperature below which heating demand starts.</summary>
    public double BalancePointC { get; init; } = 15.0;
    public double MaxKw { get; init; } = 3.0;
    /// <summary>Electrical draw per degree below the balance point.</summary>
    public RangeKw KwPerDegree { get; init; } = new(0.10, 0.15);

    public void Validate()
    {
        KwPerDegree.Validate(nameof(KwPerDegree));
        if (MaxKw <= 0) throw new InvalidOperationException("HeatPump.MaxKw must be positive.");
    }
}

public sealed class HomeChargerParameters
{
    public double PowerKw { get; init; } = 7.4;
    public RangeKw SessionKwh { get; init; } = new(8.0, 12.0);
    public double PlugInFromHour { get; init; } = 17.5;
    public double PlugInToHour { get; init; } = 19.0;
    public double DepartureHour { get; init; } = 7.0;

    public void Validate()
    {
        SessionKwh.Validate(nameof(SessionKwh));
        if (PowerKw <= 0) throw new InvalidOperationException("HomeCharger.PowerKw must be positive.");
        if (PlugInToHour < PlugInFromHour) throw new InvalidOperationException("HomeCharger plug-in window is inverted.");
    }
}

public sealed class PublicChargerParameters
{
    public double PowerKw { get; init; } = 11.0;
    public RangeKw SessionKwh { get; init; } = new(10.0, 40.0);
    /// <summary>Expected arrivals per hour, by hour-of-day band.</summary>
    public double[] ArrivalsPerHourByBand { get; init; } = [0.05, 0.20, 0.35, 0.45, 0.10];
    public double[] BandUpperHours { get; init; } = [6, 10, 15, 21, 24];

    public void Validate()
    {
        SessionKwh.Validate(nameof(SessionKwh));
        if (PowerKw <= 0) throw new InvalidOperationException("PublicCharger.PowerKw must be positive.");
        if (ArrivalsPerHourByBand.Length != BandUpperHours.Length)
            throw new InvalidOperationException("PublicCharger arrival bands and upper hours must have equal length.");
    }

    public double ArrivalsPerHour(double hour)
    {
        for (var i = 0; i < BandUpperHours.Length; i++)
            if (hour < BandUpperHours[i]) return ArrivalsPerHourByBand[i];
        return ArrivalsPerHourByBand[^1];
    }
}

/// <summary>Bindable mirror of WeatherParameters. Kept separate so the file format can change without touching the model.</summary>
public sealed class WeatherSettings
{
    public double AnnualMeanC { get; init; } = 10.0;
    public double AnnualAmplitudeC { get; init; } = 8.0;
    public int ColdestDayOfYear { get; init; } = 15;
    public double DiurnalAmplitudeC { get; init; } = 4.0;
    public double ColdestHourOfDay { get; init; } = 3.0;
    public double NoiseAmplitudeC { get; init; } = 3.0;
    public double CloudNoiseScale { get; init; } = 0.9;
    public double WinterCloudBias { get; init; } = 0.15;
    public double NoiseCorrelationHours { get; init; } = 3.0;
    public double MeanDayLengthHours { get; init; } = 12.0;
    public double DayLengthAmplitudeHours { get; init; } = 4.5;
    public int LongestDayOfYear { get; init; } = 172;
    public double ClearSkyExponent { get; init; } = 1.2;
    public double CloudAttenuation { get; init; } = 0.75;

    public WeatherParameters ToParameters() => new(
        AnnualMeanC, AnnualAmplitudeC, ColdestDayOfYear, DiurnalAmplitudeC, ColdestHourOfDay,
        NoiseAmplitudeC, CloudNoiseScale, WinterCloudBias, NoiseCorrelationHours,
        MeanDayLengthHours, DayLengthAmplitudeHours, LongestDayOfYear,
        ClearSkyExponent, CloudAttenuation);
}

public sealed class ConfiguredDailyShape(DailyShape shape) : IDailyShape
{
    public double At(double hour) => shape.At(hour);
}

public sealed class DailyShape
{
    public double Night { get; init; } = 0.55;
    public double Morning { get; init; } = 1.5;
    public double Day { get; init; } = 0.9;
    public double Evening { get; init; } = 1.8;
    public double LateEvening { get; init; } = 0.8;

    public double At(double hour) => hour switch
    {
        < 6 => Night,
        < 9 => Morning,
        < 17 => Day,
        < 22 => Evening,
        _ => LateEvening,
    };
}
