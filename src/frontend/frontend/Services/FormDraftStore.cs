using System.Collections.Concurrent;

namespace CrmAtlas.Web.Services;

/// <summary>
/// Rascunho de formulários abertos, por circuito (scoped). Permite restaurar um
/// formulário quando a página é recriada — seja por navegação do usuário, seja
/// por refresh disparado por evento em tempo real — e informa ao layout que há
/// edição em andamento para não destruir o estado da tela.
/// </summary>
public interface IFormDraftStore
{
    /// <summary>Existe algum formulário aberto agora (não suspenso).</summary>
    bool HasActive { get; }

    /// <summary>Marca o formulário como aberto e guarda a referência do modelo.</summary>
    void BeginEdit(string key, object model);

    /// <summary>Formulário salvo/cancelado: descarta o rascunho.</summary>
    void Clear(string key);

    /// <summary>Página/componente destruído com o formulário aberto: mantém o rascunho suspenso.</summary>
    void Suspend(string key);

    /// <summary>Recupera o rascunho suspenso/ativo da chave, se houver.</summary>
    T? Get<T>(string key) where T : class;

    /// <summary>Reativa um rascunho restaurado.</summary>
    void Resume(string key);
}

public sealed class FormDraftStore : IFormDraftStore
{
    private sealed record Entry(object Model, bool Active);

    private readonly ConcurrentDictionary<string, Entry> _drafts = new();

    public bool HasActive => _drafts.Values.Any(x => x.Active);

    public void BeginEdit(string key, object model) => _drafts[key] = new Entry(model, true);

    public void Clear(string key) => _drafts.TryRemove(key, out _);

    public void Suspend(string key)
    {
        if (_drafts.TryGetValue(key, out var entry))
            _drafts[key] = entry with { Active = false };
    }

    public T? Get<T>(string key) where T : class =>
        _drafts.TryGetValue(key, out var entry) ? entry.Model as T : null;

    public void Resume(string key)
    {
        if (_drafts.TryGetValue(key, out var entry))
            _drafts[key] = entry with { Active = true };
    }
}
