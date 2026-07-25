using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Operacao;

public sealed record AcompanhamentoHistoricoDto(long Id,string? Anterior,string Nova,string? Descricao,string? Responsavel,DateTime Em);
public sealed record AcompanhamentoPendenciaDto(long Id,string Label,bool Concluida,DateTime? ConcluidaEm);
public sealed record AcompanhamentoDto(long Id,long OrigemId,string Codigo,AcompanhamentoServicoTipo Tipo,string? Cliente,
    string? Endereco,string? Telefone,string? Servico,string Situacao,string? Descricao,decimal? ValorContrato,DateOnly? DataContrato,
    string? NotaFiscal,string? CondicaoPagamento,int Pendencias,int Concluidas,
    DateTime? AtualizadoEm,IReadOnlyList<AcompanhamentoHistoricoDto> Historicos,IReadOnlyList<AcompanhamentoPendenciaDto> Itens,string? CnpjCpf=null);
public sealed record AcompanhamentoImportDto(long OrigemId,string Codigo,AcompanhamentoServicoTipo Tipo,string? Cliente,
    string? Endereco,string? Telefone,string? Servico,string Situacao,string? Descricao,decimal? ValorContrato,
    DateOnly? DataContrato,string? NotaFiscal,string? CondicaoPagamento,string? CnpjCpf=null);
public sealed record AcompanhamentoImportPreviewDto(int Linha,string Aba,string Codigo,string Tipo,string? Cliente,
    bool Valido,string? Erro,AcompanhamentoImportDto? Item);
public sealed record SituacaoConfigDto(long? Id,AcompanhamentoServicoTipo Tipo,string Nome,int Ordem,bool Inicial,bool Ativo,
    IReadOnlyList<string> Pendencias);

public interface IAcompanhamentoRepository
{
    Task<IReadOnlyList<AcompanhamentoServico>> ListDetailedAsync(CancellationToken ct=default);
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
    Task<IReadOnlyList<AcompanhamentoDto>> ListAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default);
    Task<AcompanhamentoDto> GetAsync(long id,CancellationToken ct=default);
    Task<IReadOnlyList<AcompanhamentoDto>> ImportAsync(IReadOnlyList<AcompanhamentoImportDto> rows,CancellationToken ct=default);
    Task ChangeStatusAsync(long id,string novaSituacao,string? descricao,string? responsavel,CancellationToken ct=default);
    Task TogglePendingAsync(long serviceId,long pendingId,bool completed,CancellationToken ct=default);
    Task<IReadOnlyList<SituacaoConfigDto>> ListSituationsAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default);
    Task<SituacaoConfigDto> SaveSituationAsync(SituacaoConfigDto dto,CancellationToken ct=default);
}

public interface IAcompanhamentoReportService
{
    byte[] GeneratePdf(AcompanhamentoDto item);
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
    public async Task<IReadOnlyList<AcompanhamentoDto>> ListAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default)=>
        (await repository.ListDetailedAsync(ct)).Where(x=>tipo is null||x.TipoServico==tipo).OrderByDescending(x=>x.UpdatedAt).Select(Map).ToList();
    public async Task<AcompanhamentoDto> GetAsync(long id,CancellationToken ct=default)=>Map(await Find(id,ct));
    public async Task<IReadOnlyList<AcompanhamentoDto>> ImportAsync(IReadOnlyList<AcompanhamentoImportDto> rows,CancellationToken ct=default)
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
                CreatedAt=now,UpdatedAt=now,UltimaMudancaSituacaoEm=now};
            item.Historicos.Add(new(){NovaSituacao=item.Situacao,Descricao="Importação em lote",ResponsavelNome="Sistema",CreatedAt=now});
            await repository.AddAsync(item,ct);result.Add(item);
        }
        await repository.SaveChangesAsync(ct);return result.Select(Map).ToList();
    }
    public async Task ChangeStatusAsync(long id,string status,string? description,string? actor,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(status))throw new ArgumentException("A nova situação é obrigatória.");
        var item=await Find(id,ct);var now=DateTime.UtcNow;
        var config=(await repository.ListSituationsAsync(ct)).FirstOrDefault(x=>x.TipoServico==item.TipoServico&&x.Ativo&&x.Nome.Equals(status.Trim(),StringComparison.OrdinalIgnoreCase));
        item.Historicos.Add(new(){SituacaoAnterior=item.Situacao,NovaSituacao=status.Trim(),Descricao=description?.Trim(),ResponsavelNome=actor?.Trim(),CreatedAt=now});
        item.Situacao=status.Trim();item.UltimaMudancaSituacaoEm=now;item.UpdatedAt=now;
        if(config is not null)foreach(var template in config.Pendencias.Where(x=>x.Ativo&&item.Pendencias.All(y=>y.PendenciaConfigId!=x.Id)))
            item.Pendencias.Add(new(){SituacaoConfigId=config.Id,PendenciaConfigId=template.Id,Label=template.Label,CreatedAt=now,UpdatedAt=now});
        repository.Update(item);await repository.SaveChangesAsync(ct);
    }
    public async Task TogglePendingAsync(long serviceId,long pendingId,bool completed,CancellationToken ct=default)
    {
        var item=await Find(serviceId,ct);var pending=item.Pendencias.FirstOrDefault(x=>x.Id==pendingId)??throw new NotFoundException("Pendência não encontrada.");
        pending.Concluida=completed;pending.ConcluidaEm=completed?DateTime.UtcNow:null;pending.UpdatedAt=DateTime.UtcNow;item.UpdatedAt=DateTime.UtcNow;
        repository.Update(item);await repository.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<SituacaoConfigDto>> ListSituationsAsync(AcompanhamentoServicoTipo? tipo=null,CancellationToken ct=default)=>
        (await repository.ListSituationsAsync(ct)).Where(x=>tipo is null||x.TipoServico==tipo).OrderBy(x=>x.Ordem).Select(MapSituation).ToList();
    public async Task<SituacaoConfigDto> SaveSituationAsync(SituacaoConfigDto dto,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(dto.Nome))throw new ArgumentException("Nome da situação é obrigatório.");
        var entity=dto.Id is null?new AcompanhamentoServicoSituacaoConfig():await repository.GetSituationAsync(dto.Id.Value,ct)??throw new NotFoundException("Situação não encontrada.");
        entity.TipoServico=dto.Tipo;entity.Nome=dto.Nome.Trim();entity.Ordem=dto.Ordem;entity.SituacaoInicial=dto.Inicial;entity.Ativo=dto.Ativo;entity.Pendencias.Clear();
        var now=DateTime.UtcNow;foreach(var label in dto.Pendencias.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            entity.Pendencias.Add(new(){Label=label.Trim(),Ativo=true,CreatedAt=now,UpdatedAt=now});
        if(dto.Id is null)await repository.AddSituationAsync(entity,ct);else repository.UpdateSituation(entity);
        await repository.SaveChangesAsync(ct);return MapSituation(entity);
    }
    private async Task<AcompanhamentoServico> Find(long id,CancellationToken ct)=>await repository.GetDetailedAsync(id,ct)??throw new NotFoundException("Acompanhamento não encontrado.");
    private static AcompanhamentoDto Map(AcompanhamentoServico x)=>new(x.Id,x.OrigemId,x.Codigo,x.TipoServico,x.NomeCliente,
        x.Endereco,x.Telefone,x.Subtipo,x.Situacao,x.Descricao,x.ValorContrato,x.DataContrato,x.NotaFiscal,x.CondicaoPagamento,
        x.Pendencias.Count,x.Pendencias.Count(y=>y.Concluida),x.UltimaMudancaSituacaoEm,
        x.Historicos.OrderByDescending(y=>y.CreatedAt).Select(y=>new AcompanhamentoHistoricoDto(y.Id,y.SituacaoAnterior,y.NovaSituacao,y.Descricao,y.ResponsavelNome,y.CreatedAt)).ToList(),
        x.Pendencias.OrderBy(y=>y.Concluida).ThenBy(y=>y.Label).Select(y=>new AcompanhamentoPendenciaDto(y.Id,y.Label,y.Concluida,y.ConcluidaEm)).ToList(),
        x.CnpjCpf);
    private static SituacaoConfigDto MapSituation(AcompanhamentoServicoSituacaoConfig x)=>new(x.Id,x.TipoServico,x.Nome,x.Ordem??0,x.SituacaoInicial,x.Ativo,
        x.Pendencias.Where(y=>y.Ativo).OrderBy(y=>y.Ordem).Select(y=>y.Label).ToList());

}
