using System.Net;
using System.Text.Json;
using Shouldly;

namespace Sim.Api.Tests;

/// <summary>
/// The API is where every UI requirement gets its data, so these tests prove at
/// the HTTP boundary what was previously only checked by hand with curl and by
/// reading a browser. They boot the real application: real engine, real
/// background worker, real SQLite.
///
/// Split deliberately: read-only tests share one booted application, while every
/// test that MUTATES global simulation state gets its own. xUnit gives each test
/// class its own IClassFixture instance, and does not guarantee ordering within a
/// class - so a shared fixture plus a PUT means one test silently decides
/// another's starting point. That is exactly what happened on the first run here.
/// </summary>
public sealed class TheSimulationApiSpecification(SimulationApiFixture api) : IClassFixture<SimulationApiFixture>
{
    // ---------- R-33 / R-01: the application serves, and the clock is controllable ----------

    [Fact]
    public async Task Given_the_application_is_running_When_health_is_requested_Then_it_reports_ok()
    {
        var response = await api.Client.GetAsync("/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/config.html")]
    public async Task Given_the_application_is_running_When_a_page_is_requested_Then_it_is_served(string page)
    {
        var response = await api.Client.GetAsync(page);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("<");
    }

    // ---------- R-19 to R-23, R-48 to R-50: everything the UI needs is actually served ----------

    [Fact]
    public async Task Given_a_snapshot_When_it_is_read_Then_it_carries_the_clock_the_weather_and_the_season()
    {
        var snapshot = await api.GetJsonAsync("/api/simulation");

        snapshot.GetProperty("instant").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.MinValue);
        snapshot.GetProperty("season").GetString().ShouldBeOneOf("Winter", "Spring", "Summer", "Autumn");
        snapshot.GetProperty("temperatureC").GetDouble().ShouldBeInRange(-40, 50);
        snapshot.GetProperty("cloudCover").GetDouble().ShouldBeInRange(0, 1);
        snapshot.GetProperty("irradianceFactor").GetDouble().ShouldBeInRange(0, 1);
    }

    [Fact]
    public async Task Given_a_snapshot_When_it_is_read_Then_the_neighbourhood_is_exactly_thirty_houses_and_six_chargers()
    {
        var snapshot = await api.GetJsonAsync("/api/simulation");

        snapshot.GetProperty("houses").GetArrayLength().ShouldBe(30);
        snapshot.GetProperty("publicChargers").GetArrayLength().ShouldBe(6);
    }

    [Fact]
    public async Task Given_a_snapshot_When_it_is_read_Then_every_meter_reports_cumulative_energy_since_start()
    {
        // R-23: 62 asset meters plus the neighbourhood battery.
        var meters = (await api.GetJsonAsync("/api/simulation")).GetProperty("meters");

        meters.GetArrayLength().ShouldBe(63);
        foreach (var meter in meters.EnumerateArray())
        {
            meter.GetProperty("meterId").GetString().ShouldNotBeNullOrWhiteSpace();
            meter.GetProperty("consumedKwh").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
            meter.GetProperty("generatedKwh").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task Given_a_snapshot_When_the_window_is_read_Then_it_spans_the_last_24_simulated_hours()
    {
        var snapshot = await api.GetJsonAsync("/api/simulation");
        var window = snapshot.GetProperty("last24Hours");

        window.GetArrayLength().ShouldBeGreaterThan(90);   // 96 intervals at the 15 minute default

        var points = window.EnumerateArray().ToList();
        var first = points.First().GetProperty("instant").GetDateTimeOffset();
        var last = points.Last().GetProperty("instant").GetDateTimeOffset();
        (last - first).ShouldBeLessThanOrEqualTo(TimeSpan.FromHours(24.5));
        (last - first).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromHours(23.5));
    }

    [Fact]
    public async Task Given_a_snapshot_When_the_window_is_read_Then_each_point_carries_both_the_with_and_without_battery_load()
    {
        // R-48: the counterfactual is served, not computed in the browser.
        var window = (await api.GetJsonAsync("/api/simulation")).GetProperty("last24Hours");

        foreach (var point in window.EnumerateArray())
        {
            point.TryGetProperty("netKw", out _).ShouldBeTrue();
            point.TryGetProperty("netWithoutBatteryKw", out _).ShouldBeTrue();
            point.TryGetProperty("batteryKw", out _).ShouldBeTrue();
            point.GetProperty("socPercent").GetDouble().ShouldBeInRange(0, 100);
        }
    }

    [Fact]
    public async Task Given_a_battery_is_installed_When_a_snapshot_is_read_Then_its_power_and_state_of_charge_are_reported()
    {
        // R-49.
        var battery = (await api.GetJsonAsync("/api/simulation")).GetProperty("battery");

        battery.ValueKind.ShouldNotBe(JsonValueKind.Null);
        battery.GetProperty("capacityKwh").GetDouble().ShouldBeGreaterThan(0);
        battery.GetProperty("stateOfChargePercent").GetDouble().ShouldBeInRange(0, 100);
        battery.GetProperty("stateOfChargeKwh").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        battery.GetProperty("mode").GetString().ShouldBeOneOf("charging", "discharging", "idle");
        battery.GetProperty("strategy").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Given_a_snapshot_When_the_peak_figures_are_read_Then_the_battery_never_makes_the_peak_worse()
    {
        // R-50. The claim the UI makes is that the battery reduces the peak; the
        // weakest form of that which must ALWAYS hold is that it never raises it.
        var snapshot = await api.GetJsonAsync("/api/simulation");

        var without = snapshot.GetProperty("peakWithoutBatteryKw").GetDouble();
        var with = snapshot.GetProperty("peakWithBatteryKw").GetDouble();

        with.ShouldBeLessThanOrEqualTo(without + 1e-6);
    }

    [Fact]
    public async Task Given_a_snapshot_When_the_totals_are_read_Then_energy_accounting_is_present_and_consistent()
    {
        var snapshot = await api.GetJsonAsync("/api/simulation");

        snapshot.GetProperty("totalConsumedKwh").GetDouble().ShouldBeGreaterThan(0);
        snapshot.GetProperty("totalImportedKwh").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        snapshot.GetProperty("totalExportedKwh").GetDouble().ShouldBeGreaterThanOrEqualTo(0);

        // Import and export are mutually exclusive within a single interval.
        var import = snapshot.GetProperty("importKw").GetDouble();
        var export = snapshot.GetProperty("exportKw").GetDouble();
        Math.Min(import, export).ShouldBe(0, 1e-6);
    }

    // ---------- R-24: configuration through the API ----------

    [Fact]
    public async Task Given_a_configuration_When_it_is_read_Then_it_exposes_the_whole_scenario()
    {
        var configuration = await api.GetConfigurationAsync();

        foreach (var field in new[]
                 {
                     "seed", "startInstant", "tickMinutes", "ticksPerSecond",
                     "pvShare", "heatPumpShare", "homeEvShare",
                     "batteryCapacityKwh", "batteryMaxPowerKw", "batteryRoundTripEfficiency",
                     "peakShavingThresholdKw", "batteryEnabled",
                 })
            configuration.TryGetProperty(field, out _).ShouldBeTrue($"configuration is missing '{field}'");
    }

}


/// <summary>The clock is controllable, and it runs on its own. Own fixture: these tests stop and start it.</summary>
public sealed class TheSimulationClockSpecification(SimulationApiFixture api) : IClassFixture<SimulationApiFixture>
{
    [Fact]
    public async Task Given_a_running_simulation_When_it_is_paused_and_resumed_Then_the_reported_state_follows()
    {
        (await api.Client.PostAsync("/api/simulation/pause", null)).EnsureSuccessStatusCode();
        (await api.GetJsonAsync("/api/simulation")).GetProperty("running").GetBoolean().ShouldBeFalse();

        (await api.Client.PostAsync("/api/simulation/resume", null)).EnsureSuccessStatusCode();
        (await api.GetJsonAsync("/api/simulation")).GetProperty("running").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Given_a_paused_simulation_When_it_is_left_alone_Then_simulated_time_does_not_move()
    {
        (await api.Client.PostAsync("/api/simulation/pause", null)).EnsureSuccessStatusCode();

        var before = (await api.GetJsonAsync("/api/simulation")).GetProperty("tickIndex").GetInt64();
        await Task.Delay(1000);
        var after = (await api.GetJsonAsync("/api/simulation")).GetProperty("tickIndex").GetInt64();

        after.ShouldBe(before);
        (await api.Client.PostAsync("/api/simulation/resume", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Given_a_running_simulation_When_it_is_left_alone_Then_simulated_time_advances_by_itself()
    {
        // R-18: the animation is driven by the server, not by the page.
        (await api.Client.PostAsync("/api/simulation/resume", null)).EnsureSuccessStatusCode();

        var before = (await api.GetJsonAsync("/api/simulation")).GetProperty("tickIndex").GetInt64();
        await Task.Delay(1500);
        var after = (await api.GetJsonAsync("/api/simulation")).GetProperty("tickIndex").GetInt64();

        after.ShouldBeGreaterThan(before);
    }
}

/// <summary>Configuration through the API. Own fixture: every test here rebuilds the world.</summary>
public sealed class TheConfigurationApiSpecification(SimulationApiFixture api) : IClassFixture<SimulationApiFixture>
{
    [Fact]
    public async Task Given_a_new_seed_When_the_configuration_is_saved_Then_the_simulation_adopts_it()
    {
        var response = await api.PutConfigurationAsync(c => c["seed"] = 5150.0);

        response.EnsureSuccessStatusCode();
        (await api.GetConfigurationAsync()).GetProperty("seed").GetInt64().ShouldBe(5150);
    }

    [Fact]
    public async Task Given_a_hostile_configuration_When_it_is_saved_Then_the_invariants_still_hold()
    {
        var response = await api.PutConfigurationAsync(c =>
        {
            c["pvShare"] = 99.0;
            c["heatPumpShare"] = -5.0;
            c["homeEvShare"] = 12345.0;
            c["tickMinutes"] = 9999.0;
        });

        response.EnsureSuccessStatusCode();

        var snapshot = await api.GetJsonAsync("/api/simulation");
        snapshot.GetProperty("houses").GetArrayLength().ShouldBe(30);
        snapshot.GetProperty("publicChargers").GetArrayLength().ShouldBe(6);

        var configuration = await api.GetConfigurationAsync();
        configuration.GetProperty("pvShare").GetDouble().ShouldBeInRange(0, 1);
        configuration.GetProperty("heatPumpShare").GetDouble().ShouldBeInRange(0, 1);
        configuration.GetProperty("tickMinutes").GetInt32().ShouldBeInRange(1, 60);
    }

    [Fact]
    public async Task Given_the_same_seed_When_the_configuration_is_reapplied_Then_the_world_is_rebuilt_identically()
    {
        // Compares the LAYOUT - which houses exist and what is installed in them -
        // because that is what a seed determines. It deliberately does not compare
        // live power: the worker keeps ticking between the two reads, so those
        // values are a function of the tick index, not of the seed, and asserting
        // on them makes the test fail whenever the machine is busy. An earlier
        // version of this test did exactly that and was flaky under parallel load.
        await api.PutConfigurationAsync(c => { c["seed"] = 31337.0; c["pvShare"] = 0.5; });
        var first = await LayoutAsync();

        await api.PutConfigurationAsync(c => { c["seed"] = 31337.0; c["pvShare"] = 0.5; });
        var second = await LayoutAsync();

        second.ShouldBe(first);
        first.ShouldNotBeEmpty();
    }

    /// <summary>House ids and their installed asset types - everything the seed decides, and nothing the clock decides.</summary>
    private async Task<IReadOnlyList<string>> LayoutAsync() =>
        (await api.GetJsonAsync("/api/simulation")).GetProperty("houses").EnumerateArray()
            .Select(h => h.GetProperty("id").GetString() + ":" +
                         string.Join(",", h.GetProperty("assets").EnumerateArray().Select(a => a.GetString())))
            .ToList();
}

/// <summary>
/// The reset path, on its own fixture and asserting an absolute value rather than
/// a captured baseline. THE POINT: after a reset the seed must equal the one in
/// appsettings.Simulation.json, which is what proves the file is genuinely live
/// rather than decorative.
/// </summary>
public sealed class TheConfigurationResetSpecification(SimulationApiFixture api) : IClassFixture<SimulationApiFixture>
{
    /// <summary>The seed declared in src/Sim.Api/appsettings.Simulation.json.</summary>
    private const long SeedInTheConfigurationFile = 20260818;

    [Fact]
    public async Task Given_a_first_boot_When_the_configuration_is_read_Then_it_came_from_the_file()
    {
        (await api.GetConfigurationAsync()).GetProperty("seed").GetInt64().ShouldBe(SeedInTheConfigurationFile);
    }

    [Fact]
    public async Task Given_a_changed_configuration_When_it_is_reset_Then_it_returns_to_the_scenario_from_the_file()
    {
        (await api.PutConfigurationAsync(c => c["seed"] = 987654.0)).EnsureSuccessStatusCode();
        (await api.GetConfigurationAsync()).GetProperty("seed").GetInt64().ShouldBe(987654);

        (await api.Client.PostAsync("/api/simulation/configuration/reset", null)).EnsureSuccessStatusCode();

        (await api.GetConfigurationAsync()).GetProperty("seed").GetInt64().ShouldBe(SeedInTheConfigurationFile);
    }
}
