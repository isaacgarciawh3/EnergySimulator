using Sim.Application.Ports;

namespace Sim.Infrastructure.Messaging;

/// <summary>
/// The event stream we deliberately did not build (ADR-004), reduced to its
/// smallest honest form: synchronous in-process dispatch behind the same
/// publish/subscribe signature a broker client would expose.
///
/// Swapping this for Kafka, RabbitMQ or Pub/Sub is a change to THIS FILE and the
/// DI registration — no domain code moves. That is the whole point of the port.
/// </summary>
public sealed class InProcessTickBus : ITickBus
{
    private readonly List<Action<TickCompleted>> _handlers = [];
    private readonly Lock _gate = new();

    public void Subscribe(Action<TickCompleted> handler)
    {
        lock (_gate) _handlers.Add(handler);
    }

    public void Publish(TickCompleted tick)
    {
        Action<TickCompleted>[] snapshot;
        lock (_gate) snapshot = [.. _handlers];
        foreach (var handler in snapshot) handler(tick);
    }
}
