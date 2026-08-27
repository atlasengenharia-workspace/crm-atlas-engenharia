using System.Collections.Concurrent;
using CrmAtlas.ApplicationCore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CrmAtlas.Web.Realtime;

public interface ICrmRealtimeClient
{
    Task DataChanged(RealtimeChange change);
}

[Authorize]
public sealed class CrmRealtimeHub : Hub<ICrmRealtimeClient>
{
    public Task SubscribeModule(string module) =>
        Groups.AddToGroupAsync(Context.ConnectionId, NormalizeGroup(module));

    public Task UnsubscribeModule(string module) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, NormalizeGroup(module));

    private static string NormalizeGroup(string module) =>
        $"module:{module.Trim().ToLowerInvariant()}";
}

public sealed class SignalRRealtimeNotifier(
    IHubContext<CrmRealtimeHub, ICrmRealtimeClient> hub,
    ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier, IRealtimeChangeFeed
{
    private readonly ConcurrentDictionary<Guid, Func<RealtimeChange, Task>> _subscribers = new();

    public async Task PublishAsync(RealtimeChange change, CancellationToken cancellationToken = default)
    {
        try
        {
            await hub.Clients.All.DataChanged(change);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao transmitir mudança em tempo real do módulo {Module}.", change.Module);
        }

        foreach (var subscriber in _subscribers.Values)
        {
            try { await subscriber(change); }
            catch (Exception ex) { logger.LogDebug(ex, "Assinante local de tempo real foi desconectado."); }
        }
    }

    public IDisposable Subscribe(Func<RealtimeChange, Task> handler)
    {
        var id = Guid.NewGuid();
        _subscribers[id] = handler;
        return new Subscription(() => _subscribers.TryRemove(id, out _));
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) dispose();
        }
    }
}

