using System.ComponentModel.DataAnnotations;
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
    int PageSize = 20);

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

public sealed class ClienteService(IRepository<Cliente> repository) : IClienteService
{
    private static readonly Regex DocumentoPattern = new(
        @"^(LEG-[0-9A-F]{12}|\d{11}|\d{14}|\d{3}\.\d{3}\.\d{3}-\d{2}|\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2})$",
        RegexOptions.Compiled);

    public async Task<PagedResult<ClienteDto>> ListAsync(
        ClienteFilter filter,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListAsync(cancellationToken);
        var filtered = items
            .Where(x => Contains(x.CnpjCpf, filter.CnpjCpf))
            .Where(x => Contains(x.RazaoSocial, filter.RazaoSocial))
            .Where(x => Contains(x.NomeContato, filter.NomeContato))
            .Where(x => Contains(x.Telefone, filter.Telefone))
            .Where(x => Contains(x.Email, filter.Email))
            .Where(x => Contains(x.Cidade, filter.Cidade))
            .Where(x => Contains(x.Estado, filter.Estado))
            .OrderBy(x => x.RazaoSocial)
            .Select(ToDto);
        return PagedResult<ClienteDto>.Create(filtered, filter.Page, filter.PageSize);
    }

    public async Task<ClienteDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<ClienteDto> CreateAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Cliente();
        await ApplyAsync(entity, dto, null, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ClienteDto> UpdateAsync(long id, ClienteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await ApplyAsync(entity, dto, id, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClienteStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var items = await repository.ListAsync(cancellationToken);
        return new(
            items.Count,
            items.Where(x => !string.IsNullOrWhiteSpace(x.Cidade)).Select(x => x.Cidade!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            items.Where(x => !string.IsNullOrWhiteSpace(x.Estado)).Select(x => x.Estado!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count());
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
