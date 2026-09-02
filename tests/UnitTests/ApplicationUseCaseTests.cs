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
