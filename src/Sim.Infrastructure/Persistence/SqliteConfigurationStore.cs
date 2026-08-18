using Microsoft.Data.Sqlite;
using Sim.Application.Configuration;
using Sim.Application.Ports;

namespace Sim.Infrastructure.Persistence;

/// <summary>
/// SQLite adapter for <see cref="ISimulationConfigurationStore"/>. On the first
/// container start the table is empty, so the default seed is written and the
/// simulation boots from it; afterwards the configuration page overwrites it and
/// it survives restarts.
/// </summary>
public sealed class SqliteConfigurationStore(SqliteConnectionFactory factory) : ISimulationConfigurationStore
{
    public SimulationConfiguration LoadOrSeedDefault()
    {
        using var connection = factory.Open();
        using var read = connection.CreateCommand();
        read.CommandText = "SELECT seed, start_instant, tick_minutes, ticks_per_second, pv_share, heat_pump_share, home_ev_share FROM simulation_configuration WHERE id = 1;";
        using (var reader = read.ExecuteReader())
        {
            if (reader.Read())
                return new SimulationConfiguration(
                    reader.GetInt64(0),
                    DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    reader.GetInt32(2), reader.GetDouble(3),
                    reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6));
        }

        Save(SimulationConfiguration.Default);
        return SimulationConfiguration.Default;
    }

    public void Save(SimulationConfiguration configuration)
    {
        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO simulation_configuration (id, seed, start_instant, tick_minutes, ticks_per_second, pv_share, heat_pump_share, home_ev_share)
            VALUES (1, $seed, $start, $tick, $tps, $pv, $hp, $ev)
            ON CONFLICT(id) DO UPDATE SET
                seed = $seed, start_instant = $start, tick_minutes = $tick, ticks_per_second = $tps,
                pv_share = $pv, heat_pump_share = $hp, home_ev_share = $ev;
            """;
        command.Parameters.AddWithValue("$seed", configuration.Seed);
        command.Parameters.AddWithValue("$start", configuration.StartInstant.ToString("O"));
        command.Parameters.AddWithValue("$tick", configuration.TickMinutes);
        command.Parameters.AddWithValue("$tps", configuration.TicksPerSecond);
        command.Parameters.AddWithValue("$pv", configuration.PvShare);
        command.Parameters.AddWithValue("$hp", configuration.HeatPumpShare);
        command.Parameters.AddWithValue("$ev", configuration.HomeEvShare);
        command.ExecuteNonQuery();
    }
}
