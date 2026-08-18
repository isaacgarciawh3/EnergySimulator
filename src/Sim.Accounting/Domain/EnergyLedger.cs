using Sim.SharedKernel;

namespace Sim.Accounting.Domain;

/// <summary>
/// AGGREGATE ROOT of the Accounting context. It takes readings and does
/// arithmetic: cumulative energy per meter since the simulation started, and
/// settlement against the grid.
///
/// It knows nothing about houses, heat pumps, weather or batteries. A meter
/// either drew power or delivered it, and the SIGN of the reading says which.
/// That is the entire vocabulary this context needs, which is why swapping the
/// simulation for real telemetry does not touch a line of it.
/// </summary>
public sealed class EnergyLedger
{
    private readonly Dictionary<string, MeterAccount> _accounts = [];

    public KilowattHours TotalConsumed { get; private set; }
    public KilowattHours TotalGenerated { get; private set; }
    public KilowattHours TotalImported { get; private set; }
    public KilowattHours TotalExported { get; private set; }

    public IReadOnlyCollection<MeterAccount> Accounts => _accounts.Values;

    public GridSettlement Post(DateTimeOffset instant, TimeSpan duration, IReadOnlyList<PowerReading> readings)
    {
        double consumption = 0, generation = 0;

        foreach (var reading in readings)
        {
            if (!_accounts.TryGetValue(reading.MeterId, out var account))
                _accounts[reading.MeterId] = account = new MeterAccount(reading.MeterId);
            account.Post(reading, duration);

            if (reading.Power.Value >= 0) consumption += reading.Power.Value;
            else generation -= reading.Power.Value;
        }

        var net = consumption - generation;
        var import = new Kilowatts(Math.Max(0, net));
        var export = new Kilowatts(Math.Max(0, -net));

        TotalConsumed += new KilowattHours(consumption * duration.TotalHours);
        TotalGenerated += new KilowattHours(generation * duration.TotalHours);
        TotalImported += import.Over(duration);
        TotalExported += export.Over(duration);

        return new GridSettlement(instant, new Kilowatts(net), import, export,
            import.Over(duration), export.Over(duration),
            new Kilowatts(consumption), new Kilowatts(generation));
    }
}

/// <summary>Cumulative energy for one meter since the simulation started.</summary>
public sealed class MeterAccount(string meterId)
{
    public string MeterId { get; } = meterId;
    public KilowattHours Consumed { get; private set; }
    public KilowattHours Generated { get; private set; }
    public KilowattHours Net => Consumed - Generated;
    public Kilowatts LastPower { get; private set; }

    internal void Post(PowerReading reading, TimeSpan duration)
    {
        var energy = reading.Power.Over(duration);
        if (energy.Value >= 0) Consumed += energy;
        else Generated += new KilowattHours(-energy.Value);
        LastPower = reading.Power;
    }
}
