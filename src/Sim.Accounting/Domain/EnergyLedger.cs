using Sim.Accounting.Contracts;
using Sim.SharedKernel;

namespace Sim.Accounting.Domain;

/// <summary>
/// AGGREGATE ROOT of the Accounting context. Pure arithmetic over postings:
/// cumulative energy per meter since simulation start, plus settlement with the
/// grid. It has no idea how the numbers were produced — swap the whole physics
/// engine and this class is untouched (ADR-001).
/// </summary>
public sealed class EnergyLedger
{
    private readonly Dictionary<string, MeterAccount> _accounts = [];

    public KilowattHours TotalConsumed { get; private set; }
    public KilowattHours TotalGenerated { get; private set; }
    public KilowattHours TotalImported { get; private set; }
    public KilowattHours TotalExported { get; private set; }

    public IReadOnlyCollection<MeterAccount> Accounts => _accounts.Values;

    /// <summary>Posts one interval and returns its grid settlement.</summary>
    public GridSettlement Post(DateTimeOffset instant, TimeSpan duration, IReadOnlyList<EnergyEntry> entries)
    {
        double consumption = 0, generation = 0;

        foreach (var entry in entries)
        {
            if (!_accounts.TryGetValue(entry.MeterId, out var account))
                _accounts[entry.MeterId] = account = new MeterAccount(entry.MeterId, entry.OwnerId, entry.Category);
            account.Post(entry);

            if (entry.Power.Value >= 0) consumption += entry.Power.Value;
            else generation -= entry.Power.Value;
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

/// <summary>Entity inside the ledger: cumulative energy for one meter since simulation start.</summary>
public sealed class MeterAccount(string meterId, string ownerId, string category)
{
    public string MeterId { get; } = meterId;
    public string OwnerId { get; } = ownerId;
    public string Category { get; } = category;

    public KilowattHours Consumed { get; private set; }
    public KilowattHours Generated { get; private set; }
    public KilowattHours Net => Consumed - Generated;
    public Kilowatts LastPower { get; private set; }

    internal void Post(EnergyEntry entry)
    {
        if (entry.Energy.Value >= 0) Consumed += entry.Energy;
        else Generated += new KilowattHours(-entry.Energy.Value);
        LastPower = entry.Power;
    }
}
