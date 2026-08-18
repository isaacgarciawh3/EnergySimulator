using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sim.Api.Tests;

/// <summary>
/// Boots the real application in memory - real engine, real worker, real SQLite -
/// against a throwaway database file, so the API is exercised exactly as a
/// browser or any other client would exercise it.
/// </summary>
public sealed class SimulationApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"sim-api-test-{Guid.NewGuid():N}.db");

    /// <summary>
    /// UseSetting, not ConfigureAppConfiguration. The application reads its
    /// database path while Program.cs is still building its own configuration,
    /// which happens BEFORE the factory's ConfigureAppConfiguration callbacks
    /// run - so an in-memory source added there arrives too late and every
    /// fixture silently falls back to the same relative "sim.db". Two test
    /// classes then share one database, and one class's hostile configuration
    /// rewrites another class's world. That is exactly what happened here.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Simulation:DatabasePath", _databasePath);

    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// The background worker starts the engine, so the first snapshot is not
    /// available the instant the host is up. Wait for it rather than racing it.
    /// </summary>
    public async Task InitializeAsync()
    {
        Client = CreateClient();

        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                var response = await Client.GetAsync("/api/simulation");
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* still starting */ }
            await Task.Delay(100);
        }

        throw new InvalidOperationException("The simulation API never became ready.");
    }

    public async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    public async Task<JsonElement> GetConfigurationAsync() => await GetJsonAsync("/api/simulation/configuration");

    /// <summary>
    /// Sends a whole configuration record back, mutating only the named fields -
    /// the same whole-record replace the configuration page performs.
    /// </summary>
    public async Task<HttpResponseMessage> PutConfigurationAsync(Action<Dictionary<string, object?>> mutate)
    {
        var current = await GetConfigurationAsync();
        var body = current.EnumerateObject().ToDictionary(p => p.Name, p => (object?)ValueOf(p.Value));
        mutate(body);
        return await Client.PutAsJsonAsync("/api/simulation/configuration", body);
    }

    private static object? ValueOf(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.GetString(),
    };

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();
        await base.DisposeAsync();
        foreach (var file in Directory.EnumerateFiles(Path.GetTempPath(), Path.GetFileName(_databasePath) + "*"))
            try { File.Delete(file); } catch (IOException) { /* the OS still has it; harmless in a temp dir */ }
    }
}
