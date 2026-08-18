using Sim.Domain.Contracts;

namespace Sim.Domain.Accounting;

/// <summary>
/// The Accounting bounded context (ADR-001): pure arithmetic over meter
/// readings. It never references Simulation types — only the published
/// contract — and an architecture test keeps it that way.
/// </summary>
public sealed class EnergyLedger
{
    private readonly Dictionary<string, MeterAccount> _accounts = [];

    public KilowattHours TotalConsumed { get; private set; }
    public KilowattHours TotalGenerated { get; private set; }
    public KilowattHours TotalImported { get; private set; }
    public KilowattHours TotalExported { get; private set; }

    public IReadOnlyDictionary<string, MeterAccount> Accounts => _accounts;

    public void Apply(TickReport report)
    {
        foreach (var reading in report.Readings)
        {
            if (!_accounts.TryGetValue(reading.MeterId, out var account))
                _accounts[reading.MeterId] = account = new MeterAccount(reading.MeterId, reading.OwnerId, reading.Type);
            account.Apply(reading);

            if (reading.Energy.Value >= 0) TotalConsumed += reading.Energy;
            else TotalGenerated += new KilowattHours(-reading.Energy.Value);
        }

        TotalImported += report.Grid.ImportedEnergy;
        TotalExported += report.Grid.ExportedEnergy;
    }
}

/// <summary>Cumulative energy per meter since simulation start.</summary>
public sealed class MeterAccount(string meterId, string ownerId, AssetType type)
{
    public string MeterId { get; } = meterId;
    public string OwnerId { get; } = ownerId;
    public AssetType Type { get; } = type;

    public KilowattHours Consumed { get; private set; }
    public KilowattHours Generated { get; private set; }
    public KilowattHours Net => Consumed - Generated;
    public Kilowatts LastPower { get; private set; }

    internal void Apply(MeterReading reading)
    {
        if (reading.Energy.Value >= 0) Consumed += reading.Energy;
        else Generated += new KilowattHours(-reading.Energy.Value);
        LastPower = reading.Power;
    }
}
