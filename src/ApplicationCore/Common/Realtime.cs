namespace CrmAtlas.ApplicationCore.Common;

public sealed record RealtimeChange(
    string Module,
    string Action,
    string? EntityId,
    DateTimeOffset OccurredAt);

public interface IRealtimeNotifier
{
    Task PublishAsync(RealtimeChange change, CancellationToken cancellationToken = default);
}

public interface IRealtimeChangeFeed
{
    IDisposable Subscribe(Func<RealtimeChange, Task> handler);
}

