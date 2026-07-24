using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Financeiro;

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
        var service = new ClienteService(repository);
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
        Assert.Equal(1, result.TotalItems);
    }

    private sealed class MemoryRepository<TEntity>(IEnumerable<TEntity>? seed = null)
        : IRepository<TEntity> where TEntity : Entity
    {
        private readonly List<TEntity> _items = seed?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TEntity>>(_items.ToList());

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

