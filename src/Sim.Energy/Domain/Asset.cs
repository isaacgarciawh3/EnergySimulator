namespace Sim.Energy.Domain;

public enum AssetType { BaseLoad, HeatPump, Pv, HomeEvCharger, PublicEvCharger }

/// <summary>
/// A physical thing behind a meter. This is nameplate data - what the asset IS,
/// not what it is doing. How much power it draws right now is a question for
/// whoever is producing readings, which in this deployment is the Simulation
/// context and tomorrow could be a telemetry feed.
/// </summary>
/// <param name="RatedPowerKw">
/// Nameplate rating: peak kWp for PV, charging power for a charger, maximum
/// draw for a heat pump, and the average baseline for household consumption.
/// </param>
/// <param name="ResponseCoefficient">
/// Nameplate sensitivity, only meaningful for weather-driven assets: kW of
/// electrical draw per degree below the heating balance point. Zero otherwise.
/// </param>
public sealed record Asset(
    string MeterId,
    string OwnerId,
    AssetType Type,
    double RatedPowerKw,
    double ResponseCoefficient = 0);
