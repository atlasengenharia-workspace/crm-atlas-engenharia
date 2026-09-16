using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Servicos;

namespace CrmAtlas.UnitTests;

public sealed class ApplicationUseCaseTests
{
    [Fact]
    public async Task ClienteService_RejectsDuplicateDocument()
    {
        var repository = new MemoryRepository<Cliente>(
        [
            new Cliente
            {
                Id = 1,
                CnpjCpf = "12345678901",
                RazaoSocial = "Cliente existente"
            }
        ]);
        var cache = new MemoryCrmCache();
        var service = new ClienteService(repository, cache);
        var dto = new ClienteDto(
            null, "12345678901", "Novo cliente", null, null, null,
            null, null, null, null, null, null, null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task CustoIndiretoService_FiltersAndPaginates()
    {
        var repository = new MemoryRepository<CustoIndireto>(
        [
            new CustoIndireto
            {
                Id = 1,
                Data = new DateOnly(2026, 7, 1),
                Descricao = "Aluguel",
                Categoria = "Administrativo",
                Valor = 1000
            },
            new CustoIndireto
            {
                Id = 2,
                Data = new DateOnly(2026, 7, 2),
                Descricao = "Combustível",
                Categoria = "Operacional",
                Valor = 200
            }
        ]);
        var service = new CustoIndiretoService(repository);

        var result = await service.ListAsync(new(
            null, null, null, "administrativo", Page: 1, PageSize: 10));

        Assert.Single(result.Items);
        Assert.Equal("Aluguel", result.Items[0].Descricao);
        Assert.False(result.HasNext);
    }

    [Theory]
    [InlineData("Licenciamento", 1)]
    [InlineData("SRV-002", 2)]
    public async Task LancamentoService_PageSearchMatchesDescriptionOrServiceCode(
        string search,
        long expectedId)
    {
        var repository = new MemoryRepository<Lancamento>(
        [
            new Lancamento
            {
                Id = 1,
                Descricao = "Taxa de licenciamento",
                CodigoServico = "SRV-001",
                Data = new DateOnly(2026, 7, 1),
                Valor = 100,
                Tipo = LancamentoTipo.SAIDA
            },
            new Lancamento
            {
                Id = 2,
                Descricao = "Honorários",
                CodigoServico = "SRV-002",
                Data = new DateOnly(2026, 7, 2),
                Valor = 200,
                Tipo = LancamentoTipo.ENTRADA
            }
        ]);
        var service = new LancamentoService(
            repository,
            new MemoryRepository<CadastroServico>(),
            new MemoryRepository<Prestador>());

        var result = await service.ListAsync(new(
            null, null, null, null, search, search, Page: 1, PageSize: 20));

        var item = Assert.Single(result.Items);
        Assert.Equal(expectedId, item.Id);
    }

    [Fact]
    public async Task ServicoTipoConfigService_ListAsyncMergesDefaultsWhenEmpty()
    {
        var service = new ServicoTipoConfigService(
            new MemoryRepository<ServicoTipoConfig>(), new MemoryCrmCache());

        var result = await service.ListAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal(Enum.GetValues<AcompanhamentoServicoTipo>().Length, result.Count);
        Assert.All(result, x => Assert.True(x.Ativo));
        Assert.Equal("Processos administrativos",
            result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.PROCESSOS_ADM).Label);
    }

    [Fact]
    public async Task ServicoTipoConfigService_ListAsyncAppliesSavedOverridesAndOrder()
    {
        var repository = new MemoryRepository<ServicoTipoConfig>(
        [
            new ServicoTipoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "AVCB / Licenças", Ordem = 3, Ativo = true },
            new ServicoTipoConfig { Id = 2, TipoServico = AcompanhamentoServicoTipo.CLCB, Ordem = 0, Ativo = false }
        ]);
        var service = new ServicoTipoConfigService(repository, new MemoryCrmCache());

        var result = await service.ListAsync();

        Assert.Equal(AcompanhamentoServicoTipo.CLCB, result[0].TipoServico);
        Assert.False(result[0].Ativo);
        Assert.Equal("AVCB / Licenças", result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB).Label);
        Assert.Equal("CLCB", result[0].Label);

        var active = await service.ListActiveAsync();
        Assert.DoesNotContain(active, x => x.TipoServico == AcompanhamentoServicoTipo.CLCB);
    }

    [Fact]
    public async Task ServicoTipoConfigService_SaveAsyncRejectsAllInactive()
    {
        var service = new ServicoTipoConfigService(
            new MemoryRepository<ServicoTipoConfig>(), new MemoryCrmCache());
        var configs = Enum.GetValues<AcompanhamentoServicoTipo>()
            .Select((tipo, index) => new ServicoTipoConfigDto(null, tipo, null, index, false))
            .ToList();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(configs));

        Assert.Contains("ativo", exception.Message);
    }

    [Fact]
    public async Task ServicoTipoConfigService_SaveAsyncPersistsAndTrimsNome()
    {
        var repository = new MemoryRepository<ServicoTipoConfig>();
        var cache = new MemoryCrmCache();
        var service = new ServicoTipoConfigService(repository, cache);
        var configs = new List<ServicoTipoConfigDto>
        {
            new(null, AcompanhamentoServicoTipo.AVCB, "  Laudos de incêndio  ", 0, true)
        };

        await service.SaveAsync(configs);
        var result = await service.ListAsync();

        var avcb = result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB);
        Assert.Equal("Laudos de incêndio", avcb.Label);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task FormaPagamentoService_RejectsDuplicateNome()
    {
        var repository = new MemoryRepository<FormaPagamento>(
        [
            new FormaPagamento { Id = 1, Nome = "PIX" }
        ]);
        var service = new FormaPagamentoService(repository, new MemoryCrmCache());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(new FormaPagamentoDto(null, " pix ")));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task FormaPagamentoService_RequiresNome()
    {
        var service = new FormaPagamentoService(
            new MemoryRepository<FormaPagamento>(), new MemoryCrmCache());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(new FormaPagamentoDto(null, "   ")));

        Assert.Contains("obrigatório", exception.Message);
    }

    [Fact]
    public async Task FormaPagamentoService_CreateTrimsAndPersists()
    {
        var repository = new MemoryRepository<FormaPagamento>();
        var service = new FormaPagamentoService(repository, new MemoryCrmCache());

        var created = await service.CreateAsync(new FormaPagamentoDto(null, "  Dinheiro  "));

        Assert.Equal("Dinheiro", created.Nome);
        Assert.Single(repository.AsQueryable());
    }

    private sealed class MemoryCrmCache : ICrmCache
    {
        private readonly Dictionary<string, object> _store = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult(value as T);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryRepository<TEntity>(IEnumerable<TEntity>? seed = null)
        : IRepository<TEntity> where TEntity : Entity
    {
        private readonly List<TEntity> _items = seed?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<TEntity?> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().FirstOrDefault(predicate));

        public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TEntity>>(_items.ToList());

        public IQueryable<TEntity> AsQueryable() => _items.AsQueryable();

        public Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TEntity>>(query.ToList());

        public Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
            Task.FromResult(query.Count());

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity) { }
        public void Remove(TEntity entity) => _items.Remove(entity);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
