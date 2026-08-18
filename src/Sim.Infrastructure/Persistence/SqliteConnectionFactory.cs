using Microsoft.Data.Sqlite;

namespace Sim.Infrastructure.Persistence;

/// <summary>
/// Owns the SQLite file and creates the schema on first use. Schema creation is
/// idempotent so a container restart against a mounted volume is a no-op.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        EnsureSchema();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;

            CREATE TABLE IF NOT EXISTS simulation_configuration (
                id                INTEGER PRIMARY KEY CHECK (id = 1),
                seed              INTEGER NOT NULL,
                start_instant     TEXT    NOT NULL,
                tick_minutes      INTEGER NOT NULL,
                ticks_per_second  REAL    NOT NULL,
                pv_share          REAL    NOT NULL,
                heat_pump_share   REAL    NOT NULL,
                home_ev_share     REAL    NOT NULL,
                battery_capacity_kwh   REAL    NOT NULL DEFAULT 250,
                battery_max_power_kw   REAL    NOT NULL DEFAULT 80,
                battery_efficiency     REAL    NOT NULL DEFAULT 0.9,
                peak_threshold_kw      REAL    NOT NULL DEFAULT 45,
                battery_enabled        INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS tick_history (
                instant         TEXT PRIMARY KEY,
                net_kw          REAL NOT NULL,
                consumption_kw  REAL NOT NULL,
                generation_kw   REAL NOT NULL,
                net_no_battery_kw REAL NOT NULL DEFAULT 0,
                battery_kw        REAL NOT NULL DEFAULT 0,
                soc_percent       REAL NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS meter_totals (
                meter_id        TEXT PRIMARY KEY,
                owner_id        TEXT NOT NULL,
                category        TEXT NOT NULL,
                consumed_kwh    REAL NOT NULL,
                generated_kwh   REAL NOT NULL,
                net_kwh         REAL NOT NULL,
                last_power_kw   REAL NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
