using Microsoft.Data.Sqlite;
using Sim.Application.Ports;
using Sim.Application.ReadModels;

namespace Sim.Infrastructure.Persistence;

/// <summary>
/// SQLite adapter for the CQRS read side. Note what is NOT persisted: the
/// engine's own state (EV sessions, clock position). It does not need to be —
/// the simulation is deterministic, so a restart replays the same world from
/// the seed. Determinism is what buys us a trivial persistence story.
/// </summary>
public sealed class SqliteProjectionStore(SqliteConnectionFactory factory) : IProjectionStore
{
    private const int RetainedHours = 48;

    public void AppendTick(SeriesPoint point)
    {
        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO tick_history (instant, net_kw, consumption_kw, generation_kw)
            VALUES ($instant, $net, $cons, $gen)
            ON CONFLICT(instant) DO UPDATE SET net_kw = $net, consumption_kw = $cons, generation_kw = $gen;
            DELETE FROM tick_history WHERE instant < $cutoff;
            """;
        command.Parameters.AddWithValue("$instant", point.Instant.ToString("O"));
        command.Parameters.AddWithValue("$net", point.NetKw);
        command.Parameters.AddWithValue("$cons", point.ConsumptionKw);
        command.Parameters.AddWithValue("$gen", point.GenerationKw);
        command.Parameters.AddWithValue("$cutoff", point.Instant.AddHours(-RetainedHours).ToString("O"));
        command.ExecuteNonQuery();
    }

    public void SaveMeterTotals(IReadOnlyList<MeterTotalView> meters)
    {
        using var connection = factory.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO meter_totals (meter_id, owner_id, category, consumed_kwh, generated_kwh, net_kwh, last_power_kw)
            VALUES ($id, $owner, $category, $consumed, $generated, $net, $power)
            ON CONFLICT(meter_id) DO UPDATE SET
                consumed_kwh = $consumed, generated_kwh = $generated, net_kwh = $net, last_power_kw = $power;
            """;
        var id = command.Parameters.Add("$id", SqliteType.Text);
        var owner = command.Parameters.Add("$owner", SqliteType.Text);
        var category = command.Parameters.Add("$category", SqliteType.Text);
        var consumed = command.Parameters.Add("$consumed", SqliteType.Real);
        var generated = command.Parameters.Add("$generated", SqliteType.Real);
        var net = command.Parameters.Add("$net", SqliteType.Real);
        var power = command.Parameters.Add("$power", SqliteType.Real);

        foreach (var meter in meters)
        {
            id.Value = meter.MeterId; owner.Value = meter.OwnerId; category.Value = meter.Category;
            consumed.Value = meter.ConsumedKwh; generated.Value = meter.GeneratedKwh;
            net.Value = meter.NetKwh; power.Value = meter.LastPowerKw;
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public IReadOnlyList<SeriesPoint> LoadWindow(DateTimeOffset from)
    {
        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT instant, net_kw, consumption_kw, generation_kw FROM tick_history WHERE instant >= $from ORDER BY instant;";
        command.Parameters.AddWithValue("$from", from.ToString("O"));

        var points = new List<SeriesPoint>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            points.Add(new SeriesPoint(
                DateTimeOffset.Parse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3)));
        return points;
    }

    public void Reset()
    {
        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tick_history; DELETE FROM meter_totals;";
        command.ExecuteNonQuery();
    }
}
