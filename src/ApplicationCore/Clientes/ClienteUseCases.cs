using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Clientes;

public sealed record ClienteDto(
    long? Id,
    [Required] string CnpjCpf,
    [Required] string RazaoSocial,
    string? NomeContato,
    string? Telefone,
    [EmailAddress] string? Email,
    string? Rua,
    string? Numero,
    string? Bairro,
    string? Complemento,
    string? Cidade,
    [StringLength(2)] string? Estado,
    string? Cep);

public sealed record ClienteFilter(
    string? CnpjCpf,
    string? RazaoSocial,
    string? NomeContato,
    string? Telefone,
    string? Email,
    string? Cidade,
    string? Estado,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false);

public sealed record ClienteStatistics(int TotalClientes, int TotalCidades, int TotalEstados);

public sealed record CepAddress(string Cep, string? Rua, string? Bairro, string? Cidade, string? Estado);

public interface ICepLookupService
{
    Task<CepAddress?> FindAsync(string cep, CancellationToken cancellationToken = default);
}

public interface IClienteService
{
    Task<PagedResult<ClienteDto>> ListAsync(ClienteFilter filter, CancellationToken cancellationToken = default);
    Task<ClienteDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<ClienteDto> CreateAsync(ClienteDto dto, CancellationToken cancellationToken = default);
    Task<ClienteDto> UpdateAsync(long id, ClienteDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<ClienteStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public sealed class ClienteService(IRepository<Cliente> repository, ICrmCache cache) : IClienteService
{
    private static readonly Regex DocumentoPattern = new(
        @"^(LEG-[0-9A-F]{12}|\d{11}|\d{14}|\d{3}\.\d{3}\.\d{3}-\d{2}|\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2})$",
        RegexOptions.Compiled);

    public async Task<PagedResult<ClienteDto>> ListAsync(
        ClienteFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.CnpjCpf))
            query = query.Where(x => x.CnpjCpf.Contains(filter.CnpjCpf.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.RazaoSocial))
            query = query.Where(x => x.RazaoSocial.Contains(filter.RazaoSocial.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.NomeContato))
            query = query.Where(x => x.NomeContato != null && x.NomeContato.Contains(filter.NomeContato.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.Telefone))
            query = query.Where(x => x.Telefone != null && x.Telefone.Contains(filter.Telefone.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.Email))
            query = query.Where(x => x.Email != null && x.Email.Contains(filter.Email.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.Cidade))
            query = query.Where(x => x.Cidade != null && x.Cidade.Contains(filter.Cidade.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.Estado))
            query = query.Where(x => x.Estado != null && x.Estado.Contains(filter.Estado.Trim()));

        query = ApplySort(query, filter.SortKey, filter.SortDescending);

        var all = filter.PageSize == 0;
        var pageSize = all ? 0 : CursorPagination.ClampPageSize(filter.PageSize);
        var page = Math.Max(1, filter.Page);
        var total = await repository.CountAsync(query, cancellationToken);
        var items = all
            ? await repository.ToListAsync(query, cancellationToken)
            : await repository.ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);
        var dtos = items.Select(ToDto).ToList();

        return PagedResult<ClienteDto>.Create(dtos, page, all ? total : pageSize, total);
    }

    public async Task<ClienteDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<ClienteDto> CreateAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Cliente();
        await ApplyAsync(entity, dto, null, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("cliente:statistics", cancellationToken);
        return ToDto(entity);
    }

    public async Task<ClienteDto> UpdateAsync(long id, ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await ApplyAsync(entity, dto, id, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("cliente:statistics", cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("cliente:statistics", cancellationToken);
    }

    public async Task<ClienteStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        const string key = "cliente:statistics";
        var cached = await cache.GetAsync<ClienteStatistics>(key, cancellationToken);
        if (cached is not null) return cached;

        var items = await repository.ListAsync(cancellationToken);
        var stats = new ClienteStatistics(
            items.Count,
            items.Where(x => !string.IsNullOrWhiteSpace(x.Cidade)).Select(x => x.Cidade!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            items.Where(x => !string.IsNullOrWhiteSpace(x.Estado)).Select(x => x.Estado!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        await cache.SetAsync(key, stats, TimeSpan.FromHours(1), cancellationToken);
        return stats;
    }

    private async Task ApplyAsync(
        Cliente entity,
        ClienteDto dto,
        long? currentId,
        CancellationToken cancellationToken)
    {
        var documento = Required(dto.CnpjCpf, "O CNPJ/CPF é obrigatório.");
        if (!DocumentoPattern.IsMatch(documento))
            throw new ArgumentException("Informe um CPF ou CNPJ válido.");

        var all = await repository.ListAsync(cancellationToken);
        if (all.Any(x => x.Id != currentId && x.CnpjCpf.Equals(documento, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe um cliente cadastrado com este CNPJ/CPF.");

        entity.CnpjCpf = documento;
        entity.RazaoSocial = Required(dto.RazaoSocial, "A razão social é obrigatória.");
        entity.NomeContato = Clean(dto.NomeContato);
        entity.Telefone = Clean(dto.Telefone);
        entity.Email = Clean(dto.Email);
        entity.Rua = Clean(dto.Rua);
        entity.Numero = Clean(dto.Numero);
        entity.Bairro = Clean(dto.Bairro);
        entity.Complemento = Clean(dto.Complemento);
        entity.Cidade = Clean(dto.Cidade);
        entity.Estado = Clean(dto.Estado)?.ToUpperInvariant();
        entity.Cep = Clean(dto.Cep);
    }

    private async Task<Cliente> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Cliente não encontrado com id: {id}.");

    private static IQueryable<Cliente> ApplySort(IQueryable<Cliente> query, string? sortKey, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortKey)) return query.OrderByDescending(x => x.Id);

        var ordered = sortKey.ToLowerInvariant() switch
        {
            "cliente" => descending ? query.OrderByDescending(x => x.RazaoSocial) : query.OrderBy(x => x.RazaoSocial),
            "documento" => descending ? query.OrderByDescending(x => x.CnpjCpf) : query.OrderBy(x => x.CnpjCpf),
            "telefone" => descending ? query.OrderByDescending(x => x.Telefone) : query.OrderBy(x => x.Telefone),
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "cidade" => descending ? query.OrderByDescending(x => x.Cidade) : query.OrderBy(x => x.Cidade),
            "estado" => descending ? query.OrderByDescending(x => x.Estado) : query.OrderBy(x => x.Estado),
            "codigo" or _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
        };

        return ordered.ThenBy(x => x.Id);
    }

    private static ClienteDto ToDto(Cliente x) => new(
        x.Id, x.CnpjCpf, x.RazaoSocial, x.NomeContato, x.Telefone, x.Email, x.Rua,
        x.Numero, x.Bairro, x.Complemento, x.Cidade, x.Estado, x.Cep);

    private static bool Contains(string? source, string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (source?.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

    private static string Required(string? value, string message) =>
        Clean(value) ?? throw new ArgumentException(message);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
