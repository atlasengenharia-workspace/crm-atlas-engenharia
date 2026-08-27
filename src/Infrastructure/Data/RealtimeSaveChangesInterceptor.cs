using CrmAtlas.ApplicationCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CrmAtlas.Infrastructure.Data;

internal sealed class RealtimeSaveChangesInterceptor(IRealtimeNotifier notifier) : SaveChangesInterceptor
{
    private readonly Dictionary<DbContext, IReadOnlyList<RealtimeChange>> _pending = new();
    private readonly object _sync = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var changes = Take(eventData.Context);
        foreach (var change in changes)
            await notifier.PublishAsync(change, cancellationToken);
        return result;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Take(eventData.Context);
        return Task.CompletedTask;
    }

    private void Capture(DbContext? context)
    {
        if (context is null) return;
        var now = DateTimeOffset.UtcNow;
        var changes = context.ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => ToChange(x, now))
            .Where(x => x is not null)
            .Cast<RealtimeChange>()
            .GroupBy(x => new { x.Module, x.Action, x.EntityId })
            .Select(x => x.First())
            .ToList();
        if (changes.Count == 0) return;
        lock (_sync) _pending[context] = changes;
    }

    private IReadOnlyList<RealtimeChange> Take(DbContext? context)
    {
        if (context is null) return [];
        lock (_sync)
        {
            if (!_pending.Remove(context, out var changes)) return [];
            return changes;
        }
    }

    private static RealtimeChange? ToChange(EntityEntry entry, DateTimeOffset now)
    {
        var module = entry.Entity.GetType().Name switch
        {
            "Cliente" => "clientes",
            "CadastroServico" or "CadastroServicoParcela" or "CadastroServicoPrestador" => "servicos",
            "AcompanhamentoServico" or "AcompanhamentoServicoHistorico" or "AcompanhamentoServicoPendencia" => "acompanhamentos",
            "Lancamento" => "lancamentos",
            "CustoIndireto" => "custos-indiretos",
            "Orcamento" => "orcamentos",
            "Prestador" => "prestadores",
            "CondicaoPagamento" or "CondicaoPagamentoRegra" => "condicoes-pagamento",
            "Notification" or "NotificationRule" => "notificacoes",
            _ => null
        };
        if (module is null) return null;
        var id = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "Id")?.CurrentValue?.ToString();
        var action = entry.State switch
        {
            EntityState.Added => "created",
            EntityState.Deleted => "deleted",
            _ => "updated"
        };
        return new(module, action, id, now);
    }
}

internal sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task PublishAsync(RealtimeChange change, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

