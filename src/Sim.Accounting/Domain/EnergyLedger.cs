using Sim.SharedKernel;

namespace Sim.Accounting.Domain;

/// <summary>
/// AGGREGATE ROOT of the Accounting context: arithmetic over readings and
/// nothing else. A meter drew power or delivered it and the SIGN says which -
/// this context never learns what a heat pump is, which is why swapping the
/// simulation for real telemetry does not touch a line of it. Sums run in the
/// order the readings arrive: floating point addition is not associative, so a
/// stable order is what keeps the totals reproducible.
/// </summary>
public sealed class EnergyLedger
{
    private readonly Dictionary<string, MeterAccount> _accounts = [];

    private static void RefuseUnlessTheIntervalRunsForward(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new AccountingInvariantViolation(
                $"EnergyLedger.Post interval must run forward; {duration} would corrupt every accumulator.");
    }

    private void PostEachReadingToItsAccount(IReadOnlyList<PowerReading> readings, TimeSpan duration)
    {
        foreach (var reading in readings)
        {
            if (!_accounts.TryGetValue(reading.MeterId, out var account))
                _accounts[reading.MeterId] = account = new MeterAccount(reading.MeterId);
            account.Post(reading, duration);
        }
    }

    private static (double ConsumptionKw, double GenerationKw) SplitBySign(IReadOnlyList<PowerReading> readings)
    {
        double consumptionKw = 0, generationKw = 0;
        foreach (var reading in readings)
        {
            if (reading.Power.Value >= 0) consumptionKw += reading.Power.Value;
            else generationKw -= reading.Power.Value;
        }
        return (consumptionKw, generationKw);
    }

    private static GridSettlement SettleWithTheGrid(
        DateTimeOffset instant, TimeSpan duration, double consumptionKw, double generationKw)
    {
        var netKw = consumptionKw - generationKw;
        var import = new Kilowatts(Math.Max(0, netKw));
        var export = new Kilowatts(Math.Max(0, -netKw));
        return new GridSettlement(instant, new Kilowatts(netKw), import, export,
            import.Over(duration), export.Over(duration),
            new Kilowatts(consumptionKw), new Kilowatts(generationKw));
    }

    private void AccumulateTheRunningTotals(GridSettlement settlement, TimeSpan duration)
    {
        TotalConsumed += settlement.Consumption.Over(duration);
        TotalGenerated += settlement.Generation.Over(duration);
        TotalImported += settlement.ImportedEnergy;
        TotalExported += settlement.ExportedEnergy;
    }

    public KilowattHours TotalConsumed { get; private set; }
    public KilowattHours TotalGenerated { get; private set; }
    public KilowattHours TotalImported { get; private set; }
    public KilowattHours TotalExported { get; private set; }
    public IReadOnlyCollection<MeterAccount> Accounts => _accounts.Values;

    public GridSettlement Post(DateTimeOffset instant, TimeSpan duration, IReadOnlyList<PowerReading> readings)
    {
        RefuseUnlessTheIntervalRunsForward(duration);
        PostEachReadingToItsAccount(readings, duration);
        var (consumptionKw, generationKw) = SplitBySign(readings);
        var settlement = SettleWithTheGrid(instant, duration, consumptionKw, generationKw);
        AccumulateTheRunningTotals(settlement, duration);
        return settlement;
    }
}

/// <summary>Cumulative energy for one meter since the simulation started.</summary>
public sealed class MeterAccount
{
    public MeterAccount(string meterId) => MeterId = meterId;

    public string MeterId { get; }
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

/// <summary>Raised when a rule of the Accounting context would be violated. One type for the whole context: the message names the rule.</summary>
public sealed class AccountingInvariantViolation(string message) : InvalidOperationException(message);
