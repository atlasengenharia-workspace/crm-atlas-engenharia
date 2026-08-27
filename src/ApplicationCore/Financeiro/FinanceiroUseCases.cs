using System.ComponentModel.DataAnnotations;
using System.Linq;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Financeiro;

public sealed record CustoIndiretoDto(
    long? Id,
    DateOnly Data,
    [Required] string Descricao,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Valor,
    [Required] string Categoria);

public sealed record CustoIndiretoFilter(
    DateOnly? DataInicial,
    DateOnly? DataFinal,
    string? Descricao,
    string? Categoria,
    int Page = 1,
    int PageSize = 20,
    long? AfterId = null);

public sealed record LancamentoDto(
    long? Id,
    string? Codigo,
    LancamentoTipo Tipo,
    LancamentoStatus Status,
    LancamentoOrigem Origem,
    long? CadastroServicoId,
    string? CodigoServico,
    string? NomeCliente,
    long? PrestadorId,
    string? NomePrestador,
    [Required] string Descricao,
    DateOnly Data,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Valor,
    int? NumeroParcela,
    DateOnly? DataPrevistaParcela,
    string? FormaPagamento,
    string? MetodoPagamento,
    string? Plataforma,
    string? Empresa,
    string? ComprovanteUrl,
    string? ComprovanteNomeArquivo,
    string? Observacao,
    decimal Faturamento,
    decimal CustoDireto,
    decimal Lucro,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

public sealed record LancamentoFilter(
    LancamentoTipo? Tipo,
    LancamentoStatus? Status,
    DateOnly? DataInicial,
    DateOnly? DataFinal,
    string? Descricao,
    string? CodigoServico,
    int Page = 1,
    int PageSize = 20,
    long? AfterId = null);

public interface ICustoIndiretoService
{
    Task<CursorResult<CustoIndiretoDto>> ListAsync(CustoIndiretoFilter filter, CancellationToken cancellationToken = default);
    Task<CustoIndiretoDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<CustoIndiretoDto> CreateAsync(CustoIndiretoDto dto, CancellationToken cancellationToken = default);
    Task<CustoIndiretoDto> UpdateAsync(long id, CustoIndiretoDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustoIndiretoDto>> ImportAsync(IReadOnlyList<CustoIndiretoDto> rows, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public interface ILancamentoService
{
    Task<CursorResult<LancamentoDto>> ListAsync(LancamentoFilter filter, CancellationToken cancellationToken = default);
    Task<LancamentoDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<LancamentoDto> CreateAsync(LancamentoDto dto, CancellationToken cancellationToken = default);
    Task<LancamentoDto> UpdateAsync(long id, LancamentoDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed record StoredReceipt(string Key, string OriginalFileName, string ContentType);

public interface IReceiptStorage
{
    Task<StoredReceipt> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)?> OpenReadAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class CustoIndiretoService(IRepository<CustoIndireto> repository) : ICustoIndiretoService
{
    public async Task<CursorResult<CustoIndiretoDto>> ListAsync(
        CustoIndiretoFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();

        if (filter.DataInicial is not null)
            query = query.Where(x => x.Data >= filter.DataInicial);
        if (filter.DataFinal is not null)
            query = query.Where(x => x.Data <= filter.DataFinal);
        if (!string.IsNullOrWhiteSpace(filter.Descricao))
            query = query.Where(x => x.Descricao.ToLower().Contains(filter.Descricao.Trim().ToLower()));
        if (!string.IsNullOrWhiteSpace(filter.Categoria))
            query = query.Where(x => x.Categoria.ToLower().Contains(filter.Categoria.Trim().ToLower()));

        if (filter.AfterId is not null)
            query = query.Where(x => x.Id < filter.AfterId);

        query = query.OrderByDescending(x => x.Id);

        var pageSize = CursorPagination.ClampPageSize(filter.PageSize);
        var items = await repository.ToListAsync(query.Take(pageSize + 1), cancellationToken);
        var hasNext = items.Count > pageSize;
        var nextCursor = hasNext ? items[pageSize - 1].Id : (long?)null;
        var dtos = items.Take(pageSize).Select(ToDto).ToList();

        return new CursorResult<CustoIndiretoDto>(dtos, filter.Page, pageSize, nextCursor, hasNext);
    }

    public async Task<CustoIndiretoDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<CustoIndiretoDto> CreateAsync(CustoIndiretoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new CustoIndireto();
        Apply(entity, dto);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CustoIndiretoDto> UpdateAsync(long id, CustoIndiretoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        Apply(entity, dto);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<CustoIndiretoDto>> ImportAsync(
        IReadOnlyList<CustoIndiretoDto> rows,
        CancellationToken cancellationToken = default)
    {
        var entities = rows.Select(dto =>
        {
            var entity = new CustoIndireto();
            Apply(entity, dto);
            return entity;
        }).ToList();
        foreach (var entity in entities)
            await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustoIndireto> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Custo indireto não encontrado com id: {id}.");

    private static void Apply(CustoIndireto entity, CustoIndiretoDto dto)
    {
        if (dto.Valor <= 0) throw new ArgumentException("O valor deve ser maior que zero.");
        entity.Data = dto.Data;
        entity.Descricao = Required(dto.Descricao, "A descrição é obrigatória.");
        entity.Valor = dto.Valor;
        entity.Categoria = Required(dto.Categoria, "A categoria é obrigatória.");
    }

    private static CustoIndiretoDto ToDto(CustoIndireto x) =>
        new(x.Id, x.Data, x.Descricao, x.Valor, x.Categoria);

    private static bool Contains(string? source, string? value) =>
        string.IsNullOrWhiteSpace(value) || (source?.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();
}

public sealed class LancamentoService(
    IRepository<Lancamento> repository,
    IRepository<CrmAtlas.ApplicationCore.Servicos.CadastroServico> cadastros,
    IRepository<CrmAtlas.ApplicationCore.Servicos.Prestador> prestadores) : ILancamentoService
{
    public async Task<CursorResult<LancamentoDto>> ListAsync(
        LancamentoFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();

        if (filter.Tipo is not null)
            query = query.Where(x => x.Tipo == filter.Tipo);
        if (filter.Status is not null)
            query = query.Where(x => x.Status == filter.Status);
        if (filter.DataInicial is not null)
            query = query.Where(x => x.Data >= filter.DataInicial);
        if (filter.DataFinal is not null)
            query = query.Where(x => x.Data <= filter.DataFinal);

        var descricao = filter.Descricao?.Trim().ToLowerInvariant();
        var codigoServico = filter.CodigoServico?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(descricao) || !string.IsNullOrWhiteSpace(codigoServico))
        {
            bool same = string.Equals(descricao, codigoServico, StringComparison.OrdinalIgnoreCase);
            if (same)
            {
                query = query.Where(x =>
                    (x.Descricao != null && x.Descricao.ToLower().Contains(descricao!))
                    || (x.CodigoServico != null && x.CodigoServico.ToLower().Contains(codigoServico!)));
            }
            else
            {
                query = query.Where(x =>
                    (string.IsNullOrWhiteSpace(descricao) || (x.Descricao != null && x.Descricao.ToLower().Contains(descricao!)))
                    && (string.IsNullOrWhiteSpace(codigoServico) || (x.CodigoServico != null && x.CodigoServico.ToLower().Contains(codigoServico!))));
            }
        }

        if (filter.AfterId is not null)
            query = query.Where(x => x.Id < filter.AfterId);

        query = query.OrderByDescending(x => x.Id);

        var pageSize = CursorPagination.ClampPageSize(filter.PageSize);
        var items = await repository.ToListAsync(query.Take(pageSize + 1), cancellationToken);
        var hasNext = items.Count > pageSize;
        var nextCursor = hasNext ? items[pageSize - 1].Id : (long?)null;
        var dtos = items.Take(pageSize).Select(ToDto).ToList();

        return new CursorResult<LancamentoDto>(dtos, filter.Page, pageSize, nextCursor, hasNext);
    }

    public async Task<LancamentoDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<LancamentoDto> CreateAsync(LancamentoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Lancamento
        {
            Codigo = await NextCodeAsync(cancellationToken),
            Origem = dto.Origem
        };
        await ApplyAsync(entity, dto, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LancamentoDto> UpdateAsync(long id, LancamentoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await ApplyAsync(entity, dto, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(Lancamento entity, LancamentoDto dto, CancellationToken cancellationToken)
    {
        if (dto.Valor <= 0) throw new ArgumentException("O valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(dto.Descricao)) throw new ArgumentException("A descrição é obrigatória.");
        if (dto.CadastroServicoId is not null
            && await cadastros.GetByIdAsync(dto.CadastroServicoId.Value, cancellationToken) is null)
            throw new NotFoundException($"Cadastro de serviço não encontrado com id: {dto.CadastroServicoId}.");
        if (dto.PrestadorId is not null
            && await prestadores.GetByIdAsync(dto.PrestadorId.Value, cancellationToken) is null)
            throw new NotFoundException($"Prestador não encontrado com id: {dto.PrestadorId}.");

        entity.Tipo = dto.Tipo;
        entity.Status = dto.Status;
        entity.Origem = dto.Origem;
        entity.CadastroServicoId = dto.CadastroServicoId;
        entity.PrestadorId = dto.PrestadorId;
        entity.CodigoServico = Clean(dto.CodigoServico);
        entity.NomeCliente = Clean(dto.NomeCliente);
        entity.NomePrestador = Clean(dto.NomePrestador);
        entity.Descricao = dto.Descricao.Trim();
        entity.Valor = dto.Valor;
        entity.Data = dto.Data;
        entity.NumeroParcela = dto.NumeroParcela;
        entity.DataPrevistaParcela = dto.DataPrevistaParcela;
        entity.FormaPagamento = Clean(dto.FormaPagamento);
        entity.MetodoPagamento = Clean(dto.MetodoPagamento);
        entity.Plataforma = Clean(dto.Plataforma);
        entity.Empresa = Clean(dto.Empresa);
        entity.ComprovanteUrl = Clean(dto.ComprovanteUrl);
        entity.ComprovanteNomeArquivo = Clean(dto.ComprovanteNomeArquivo);
        entity.Observacao = Clean(dto.Observacao);
    }

    private async Task<string> NextCodeAsync(CancellationToken cancellationToken) =>
        $"L-{(await repository.ListAsync(cancellationToken)).Count + 1:000000}";

    private async Task<Lancamento> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Lançamento não encontrado com id: {id}.");

    private static LancamentoDto ToDto(Lancamento x)
    {
        var valor = x.Valor ?? 0;
        return new(
            x.Id, x.Codigo, x.Tipo, x.Status, x.Origem, x.CadastroServicoId, x.CodigoServico,
            x.NomeCliente, x.PrestadorId, x.NomePrestador, x.Descricao ?? string.Empty,
            x.Data ?? default, valor, x.NumeroParcela, x.DataPrevistaParcela, x.FormaPagamento,
            x.MetodoPagamento, x.Plataforma, x.Empresa, x.ComprovanteUrl,
            x.ComprovanteNomeArquivo, x.Observacao,
            x.Tipo == LancamentoTipo.ENTRADA ? valor : 0,
            x.Tipo == LancamentoTipo.SAIDA ? valor : 0,
            x.Tipo == LancamentoTipo.ENTRADA ? valor : -valor,
            x.CreatedAt, x.UpdatedAt);
    }

    private static bool Contains(string? source, string? value) =>
        string.IsNullOrWhiteSpace(value) || (source?.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool MatchesTextFilter(Lancamento lancamento, LancamentoFilter filter)
    {
        var descricao = filter.Descricao?.Trim();
        var codigoServico = filter.CodigoServico?.Trim();

        // The launches page sends its single search term in both fields because
        // it represents "description OR service code".
        if (!string.IsNullOrWhiteSpace(descricao)
            && string.Equals(descricao, codigoServico, StringComparison.OrdinalIgnoreCase))
            return Contains(lancamento.Descricao, descricao)
                || Contains(lancamento.CodigoServico, codigoServico);

        return Contains(lancamento.Descricao, descricao)
            && Contains(lancamento.CodigoServico, codigoServico);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
