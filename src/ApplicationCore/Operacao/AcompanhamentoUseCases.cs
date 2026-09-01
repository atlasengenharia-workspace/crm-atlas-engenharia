using System.Threading;
using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Operacao;

public sealed record AcompanhamentoFilter(
    string? Search = null,
    AcompanhamentoServicoTipo? Tipo = null,
    bool HideCompleted = false,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false);

public sealed record AcompanhamentoHistoricoDto(long Id,string? Anterior,string Nova,string? Descricao,string? Responsavel,DateTime Em);
public sealed record AcompanhamentoPendenciaDto(long Id,string Label,bool Concluida,DateTime? ConcluidaEm);
public sealed record AcompanhamentoDto(long Id,long OrigemId,string Codigo,AcompanhamentoServicoTipo Tipo,string? Cliente,
    string? Endereco,string? Telefone,string? Servico,string Situacao,string? Descricao,string? ObservacaoMudanca,decimal? ValorContrato,DateOnly? DataContrato,
    string? NotaFiscal,string? CondicaoPagamento,int Pendencias,int Concluidas,
    DateTime? AtualizadoEm,IReadOnlyList<AcompanhamentoHistoricoDto> Historicos,IReadOnlyList<AcompanhamentoPendenciaDto> Itens,string? CnpjCpf=null,
    decimal? AReceber=null,decimal? Recebido=null,decimal? Custos=null,DateOnly? ProximaParcela=null,string? ProximaParcelaTexto=null);
public sealed record AcompanhamentoImportDto(long OrigemId,string Codigo,AcompanhamentoServicoTipo Tipo,string? Cliente,
    string? Endereco,string? Telefone,string? Servico,string Situacao,string? Descricao,decimal? ValorContrato,
    DateOnly? DataContrato,string? NotaFiscal,string? CondicaoPagamento,string? CnpjCpf=null,
    decimal? AReceber=null,decimal? Recebido=null,decimal? Custos=null,DateOnly? ProximaParcela=null,string? ProximaParcelaTexto=null);
public sealed record AcompanhamentoImportPreviewDto(int Linha,string Aba,string Codigo,string Tipo,string? Cliente,
    bool Valido,string? Erro,AcompanhamentoImportDto? Item);
public sealed record SituacaoConfigDto(long? Id,AcompanhamentoServicoTipo Tipo,string Nome,int Ordem,bool Inicial,bool Ativo,
    IReadOnlyList<string> Pendencias,string? Cor=null);

public interface IAcompanhamentoRepository
{
    Task<IReadOnlyList<AcompanhamentoServico>> ListDetailedAsync(CancellationToken ct=default);
    IQueryable<AcompanhamentoServico> AsQueryable();
    Task<IReadOnlyList<AcompanhamentoServico>> ToListAsync(IQueryable<AcompanhamentoServico> query,CancellationToken ct=default);
    Task<int> CountAsync(IQueryable<AcompanhamentoServico> query,CancellationToken ct=default);
    Task<AcompanhamentoServico?> GetDetailedAsync(long id,CancellationToken ct=default);
    Task<IReadOnlyList<AcompanhamentoServicoSituacaoConfig>> ListSituationsAsync(CancellationToken ct=default);
    Task<AcompanhamentoServicoSituacaoConfig?> GetSituationAsync(long id,CancellationToken ct=default);
    Task AddAsync(AcompanhamentoServico entity,CancellationToken ct=default);
    Task AddSituationAsync(AcompanhamentoServicoSituacaoConfig entity,CancellationToken ct=default);
    void Update(AcompanhamentoServico entity);
    void UpdateSituation(AcompanhamentoServicoSituacaoConfig entity);
    Task SaveChangesAsync(CancellationToken ct=default);
}

public interface IAcompanhamentoService
{
    Task<PagedResult<AcompanhamentoDto>> ListAsync(AcompanhamentoFilter? filter=null,CancellationToken ct=default);
    Task<AcompanhamentoDto> GetAsync(long id,CancellationToken ct=default);
    Task<IReadOnlyList<AcompanhamentoDto>> ImportAsync(IReadOnlyList<AcompanhamentoImportDto> rows,CancellationToken ct=default);
    Task ChangeStatusAsync(long id,string novaSituacao,string? descricao,string? responsavel,CancellationToken ct=default);
    Task UpdateDescricaoAsync(long id,string? descricao,CancellationToken ct=default);
    Task BulkUpdateAsync(IReadOnlyList<long> ids,string? situacao,string? descricao,string? responsavel,CancellationToken ct=default);
    Task TogglePendingAsync(long serviceId,long pendingId,bool completed,CancellationToken ct=default);
    Task<IReadOnlyList<SituacaoConfigDto>> ListSituationsAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default);
    Task<SituacaoConfigDto> SaveSituationAsync(SituacaoConfigDto dto,CancellationToken ct=default);
}

public interface IAcompanhamentoReportService
{
    byte[] GeneratePdf(AcompanhamentoDto item);
    byte[] GenerateExcel(IEnumerable<AcompanhamentoDto> items);
    byte[] GenerateGeneralOperationalReport(IEnumerable<AcompanhamentoDto> items);
    byte[] GeneratePurchaseOrderReport(string prestador, string escopo, decimal valor, string condicao);
    byte[] GenerateFinancialSummaryReport(decimal faturamento, decimal custos, decimal lucro, int totalLancamentos);
}

public interface IAcompanhamentoSpreadsheetReader
{
    Task<IReadOnlyList<AcompanhamentoImportPreviewDto>> ReadAsync(
        Stream stream,
        string fileName,
        AcompanhamentoServicoTipo? tipo = null,
        CancellationToken ct = default);
}

public sealed class AcompanhamentoService(IAcompanhamentoRepository repository) : IAcompanhamentoService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<PagedResult<AcompanhamentoDto>> ListAsync(AcompanhamentoFilter? filter=null,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            var query = repository.AsQueryable();

            if (filter?.Tipo is not null)
                query = query.Where(x => x.TipoServico == filter.Tipo);

            if (filter?.HideCompleted == true)
                query = query.Where(x => !x.Situacao.StartsWith("Concluído") && !x.Situacao.StartsWith("Concluido"));

            if (!string.IsNullOrWhiteSpace(filter?.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(x =>
                    x.Codigo.Contains(term) ||
                    (x.NomeCliente != null && x.NomeCliente.Contains(term)) ||
                    (x.CnpjCpf != null && x.CnpjCpf.Contains(term)) ||
                    (x.Endereco != null && x.Endereco.Contains(term)) ||
                    (x.Telefone != null && x.Telefone.Contains(term)) ||
                    (x.Subtipo != null && x.Subtipo.Contains(term)) ||
                    x.Situacao.Contains(term) ||
                    (x.Descricao != null && x.Descricao.Contains(term)) ||
                    (x.ProximaParcelaTexto != null && x.ProximaParcelaTexto.Contains(term)));
            }

            query = ApplySort(query, filter?.SortKey, filter?.SortDescending ?? false);

            var all = filter?.PageSize == 0;
            var pageSize = all ? 0 : CursorPagination.ClampPageSize(filter?.PageSize ?? 20);
            var page = Math.Max(1, filter?.Page ?? 1);
            var total = await repository.CountAsync(query, ct);
            var items = all
                ? await repository.ToListAsync(query, ct)
                : await repository.ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), ct);
            var dtos = items.Select(Map).ToList();

            return PagedResult<AcompanhamentoDto>.Create(dtos, page, all ? total : pageSize, total);
        }, ct);

    public Task<AcompanhamentoDto> GetAsync(long id,CancellationToken ct=default)
        => ExecuteAsync(async () => Map(await Find(id,ct)), ct);

    public Task<IReadOnlyList<AcompanhamentoDto>> ImportAsync(IReadOnlyList<AcompanhamentoImportDto> rows,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            var saved=await repository.ListDetailedAsync(ct);
            if(rows.GroupBy(x=>x.Codigo.Trim(),StringComparer.OrdinalIgnoreCase).Any(x=>x.Count()>1))
                throw new ArgumentException("O arquivo possui códigos duplicados.");
            if(rows.Any(x=>saved.Any(y=>y.Codigo.Equals(x.Codigo.Trim(),StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("Um ou mais códigos já foram importados.");
            var result=new List<AcompanhamentoServico>();var now=DateTime.UtcNow;
            foreach(var row in rows)
            {
                if(string.IsNullOrWhiteSpace(row.Codigo)||string.IsNullOrWhiteSpace(row.Situacao))throw new ArgumentException("Código e situação são obrigatórios.");
                var item=new AcompanhamentoServico{OrigemId=row.OrigemId,Codigo=row.Codigo.Trim(),TipoServico=row.Tipo,
                    NomeCliente=row.Cliente?.Trim(),CnpjCpf=row.CnpjCpf?.Trim(),Endereco=row.Endereco?.Trim(),Telefone=row.Telefone?.Trim(),Subtipo=row.Servico?.Trim(),
                    Situacao=row.Situacao.Trim(),Descricao=row.Descricao?.Trim(),ValorContrato=row.ValorContrato,DataContrato=row.DataContrato,
                    NotaFiscal=row.NotaFiscal?.Trim(),CondicaoPagamento=row.CondicaoPagamento?.Trim(),
                    AReceber=row.AReceber,Recebido=row.Recebido,Custos=row.Custos,ProximaParcela=row.ProximaParcela,ProximaParcelaTexto=row.ProximaParcelaTexto,
                    CreatedAt=now,UpdatedAt=now,UltimaMudancaSituacaoEm=now};
                item.Historicos.Add(new(){NovaSituacao=item.Situacao,Descricao="Importação em lote",ResponsavelNome="Sistema",CreatedAt=now});
                await repository.AddAsync(item,ct);result.Add(item);
            }
            await repository.SaveChangesAsync(ct);
            return (IReadOnlyList<AcompanhamentoDto>)result.Select(Map).ToList();
        }, ct);

    public Task ChangeStatusAsync(long id,string status,string? description,string? actor,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            if(string.IsNullOrWhiteSpace(status))throw new ArgumentException("A nova situação é obrigatória.");
            var item=await Find(id,ct);var now=DateTime.UtcNow;
            var config=(await repository.ListSituationsAsync(ct)).FirstOrDefault(x=>x.TipoServico==item.TipoServico&&x.Ativo&&x.Nome.Equals(status.Trim(),StringComparison.OrdinalIgnoreCase));
            item.Historicos.Add(new(){SituacaoAnterior=item.Situacao,NovaSituacao=status.Trim(),Descricao=description?.Trim(),ResponsavelNome=actor?.Trim(),CreatedAt=now});
            item.Situacao=status.Trim();item.UltimaMudancaSituacaoEm=now;item.UpdatedAt=now;
            if(config is not null)foreach(var template in config.Pendencias.Where(x=>x.Ativo&&item.Pendencias.All(y=>y.PendenciaConfigId!=x.Id)))
                item.Pendencias.Add(new(){SituacaoConfigId=config.Id,PendenciaConfigId=template.Id,Label=template.Label,CreatedAt=now,UpdatedAt=now});
            repository.Update(item);await repository.SaveChangesAsync(ct);
        }, ct);

    public Task UpdateDescricaoAsync(long id,string? descricao,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            var item=await Find(id,ct);
            item.Descricao=descricao?.Trim();
            item.UpdatedAt=DateTime.UtcNow;
            repository.Update(item);await repository.SaveChangesAsync(ct);
        }, ct);

    public Task BulkUpdateAsync(IReadOnlyList<long> ids,string? situacao,string? descricao,string? responsavel,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            if(ids.Count==0)return;
            var items=(await repository.ListDetailedAsync(ct)).Where(x=>ids.Contains(x.Id)).ToList();
            var allSituations=await repository.ListSituationsAsync(ct);
            var now=DateTime.UtcNow;
            foreach(var item in items)
            {
                if(!string.IsNullOrWhiteSpace(situacao)&&!string.Equals(item.Situacao,situacao.Trim(),StringComparison.OrdinalIgnoreCase))
                {
                    var config=allSituations.FirstOrDefault(x=>x.TipoServico==item.TipoServico&&x.Ativo&&x.Nome.Equals(situacao.Trim(),StringComparison.OrdinalIgnoreCase));
                    item.Historicos.Add(new(){SituacaoAnterior=item.Situacao,NovaSituacao=situacao.Trim(),ResponsavelNome=responsavel?.Trim(),CreatedAt=now});
                    item.Situacao=situacao.Trim();item.UltimaMudancaSituacaoEm=now;
                    if(config is not null)foreach(var template in config.Pendencias.Where(x=>x.Ativo&&item.Pendencias.All(y=>y.PendenciaConfigId!=x.Id)))
                        item.Pendencias.Add(new(){SituacaoConfigId=config.Id,PendenciaConfigId=template.Id,Label=template.Label,CreatedAt=now,UpdatedAt=now});
                }
                if(descricao is not null)item.Descricao=descricao.Trim();
                item.UpdatedAt=now;
                repository.Update(item);
            }
            await repository.SaveChangesAsync(ct);
        }, ct);

    public Task TogglePendingAsync(long serviceId,long pendingId,bool completed,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            var item=await Find(serviceId,ct);var pending=item.Pendencias.FirstOrDefault(x=>x.Id==pendingId)??throw new NotFoundException("Pendência não encontrada.");
            pending.Concluida=completed;pending.ConcluidaEm=completed?DateTime.UtcNow:null;pending.UpdatedAt=DateTime.UtcNow;item.UpdatedAt=DateTime.UtcNow;
            repository.Update(item);await repository.SaveChangesAsync(ct);
        }, ct);

    public Task<IReadOnlyList<SituacaoConfigDto>> ListSituationsAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default)
        => ExecuteAsync(async () =>
            (IReadOnlyList<SituacaoConfigDto>)(await repository.ListSituationsAsync(ct)).Where(x=>tipo is null||x.TipoServico==tipo).OrderBy(x=>x.Ordem).Select(MapSituation).ToList(), ct);

    public Task<SituacaoConfigDto> SaveSituationAsync(SituacaoConfigDto dto,CancellationToken ct=default)
        => ExecuteAsync(async () =>
        {
            if(string.IsNullOrWhiteSpace(dto.Nome))throw new ArgumentException("Nome da situação é obrigatório.");
            var entity=dto.Id is null?new AcompanhamentoServicoSituacaoConfig():await repository.GetSituationAsync(dto.Id.Value,ct)??throw new NotFoundException("Situação não encontrada.");
            entity.TipoServico=dto.Tipo;entity.Nome=dto.Nome.Trim();entity.Ordem=dto.Ordem;entity.SituacaoInicial=dto.Inicial;entity.Ativo=dto.Ativo;entity.Cor=NormalizeColor(dto.Cor);entity.Pendencias.Clear();
            var now=DateTime.UtcNow;foreach(var label in dto.Pendencias.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                entity.Pendencias.Add(new(){Label=label.Trim(),Ativo=true,CreatedAt=now,UpdatedAt=now});
            if(dto.Id is null)await repository.AddSituationAsync(entity,ct);else repository.UpdateSituation(entity);
            await repository.SaveChangesAsync(ct);return MapSituation(entity);
        }, ct);

    private async Task<AcompanhamentoServico> Find(long id,CancellationToken ct)=>await repository.GetDetailedAsync(id,ct)??throw new NotFoundException("Acompanhamento não encontrado.");

    private async Task ExecuteAsync(Func<Task> action, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try { await action(); }
        finally { _semaphore.Release(); }
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try { return await action(); }
        finally { _semaphore.Release(); }
    }

    private static IQueryable<AcompanhamentoServico> ApplySort(IQueryable<AcompanhamentoServico> query, string? sortKey, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortKey)) return query.OrderByDescending(x => x.Id);

        var ordered = sortKey.ToLowerInvariant() switch
        {
            "codigo" => descending ? query.OrderByDescending(x => x.Codigo) : query.OrderBy(x => x.Codigo),
            "cliente" => descending ? query.OrderByDescending(x => x.NomeCliente) : query.OrderBy(x => x.NomeCliente),
            "endereco" => descending ? query.OrderByDescending(x => x.Endereco) : query.OrderBy(x => x.Endereco),
            "telefone" => descending ? query.OrderByDescending(x => x.Telefone) : query.OrderBy(x => x.Telefone),
            "servico" => descending ? query.OrderByDescending(x => x.Subtipo) : query.OrderBy(x => x.Subtipo),
            "valorcontrato" => descending ? query.OrderByDescending(x => x.ValorContrato) : query.OrderBy(x => x.ValorContrato),
            "datacontrato" => descending ? query.OrderByDescending(x => x.DataContrato) : query.OrderBy(x => x.DataContrato),
            "nf" => descending ? query.OrderByDescending(x => x.NotaFiscal) : query.OrderBy(x => x.NotaFiscal),
            "condicaopagamento" => descending ? query.OrderByDescending(x => x.CondicaoPagamento) : query.OrderBy(x => x.CondicaoPagamento),
            "proximaparcela" => descending ? query.OrderByDescending(x => x.ProximaParcela) : query.OrderBy(x => x.ProximaParcela),
            "situacao" => descending ? query.OrderByDescending(x => x.Situacao) : query.OrderBy(x => x.Situacao),
            "observacoes" => descending ? query.OrderByDescending(x => x.Descricao) : query.OrderBy(x => x.Descricao),
            "observacaomudanca" => descending
                ? query.OrderByDescending(x => x.Historicos.OrderByDescending(h => h.CreatedAt).Select(h => h.Descricao).FirstOrDefault())
                : query.OrderBy(x => x.Historicos.OrderByDescending(h => h.CreatedAt).Select(h => h.Descricao).FirstOrDefault()),
            "areceber" => descending ? query.OrderByDescending(x => x.AReceber) : query.OrderBy(x => x.AReceber),
            "recebido" => descending ? query.OrderByDescending(x => x.Recebido) : query.OrderBy(x => x.Recebido),
            "custos" => descending ? query.OrderByDescending(x => x.Custos) : query.OrderBy(x => x.Custos),
            "atualizacao" => descending ? query.OrderByDescending(x => x.UltimaMudancaSituacaoEm) : query.OrderBy(x => x.UltimaMudancaSituacaoEm),
            _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
        };

        return ordered.ThenBy(x => x.Id);
    }

    private static AcompanhamentoDto Map(AcompanhamentoServico x)
    {
        var historicos = x.Historicos.OrderByDescending(y => y.CreatedAt).ToList();
        return new(x.Id, x.OrigemId, x.Codigo, x.TipoServico, x.NomeCliente,
            x.Endereco, x.Telefone, x.Subtipo, x.Situacao, x.Descricao, historicos.FirstOrDefault()?.Descricao, x.ValorContrato, x.DataContrato, x.NotaFiscal, x.CondicaoPagamento,
            x.Pendencias.Count, x.Pendencias.Count(y => y.Concluida), x.UltimaMudancaSituacaoEm,
            historicos.Select(y => new AcompanhamentoHistoricoDto(y.Id, y.SituacaoAnterior, y.NovaSituacao, y.Descricao, y.ResponsavelNome, y.CreatedAt)).ToList(),
            x.Pendencias.OrderBy(y => y.Concluida).ThenBy(y => y.Label).Select(y => new AcompanhamentoPendenciaDto(y.Id, y.Label, y.Concluida, y.ConcluidaEm)).ToList(),
            x.CnpjCpf, x.AReceber, x.Recebido, x.Custos, x.ProximaParcela, x.ProximaParcelaTexto);
    }
    private static SituacaoConfigDto MapSituation(AcompanhamentoServicoSituacaoConfig x)=>new(x.Id,x.TipoServico,x.Nome,x.Ordem??0,x.SituacaoInicial,x.Ativo,
        x.Pendencias.Where(y=>y.Ativo).OrderBy(y=>y.Ordem).Select(y=>y.Label).ToList(),x.Cor);
    private static string? NormalizeColor(string? color)
    {
        if(string.IsNullOrWhiteSpace(color))return null;
        var value=color.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$")?value.ToUpperInvariant():throw new ArgumentException("Informe uma cor hexadecimal válida.");
    }

}
