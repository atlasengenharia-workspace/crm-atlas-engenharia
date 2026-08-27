using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Notificacoes;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Identidade;

namespace CrmAtlas.ApplicationCore.Operacao;

public sealed record OrcamentoDto(long? Id, [Required] string Codigo, string? Nome, string? Descricao,
    [Required] string Situacao, string? Telefone, AcompanhamentoServicoTipo TipoServico, decimal? ValorTotal,
    DateOnly? Data = null, [EmailAddress] string? Email = null);

public interface IOrcamentoService
{
    Task<IReadOnlyList<OrcamentoDto>> ListAsync(CancellationToken ct = default);
    Task<OrcamentoDto> SaveAsync(OrcamentoDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class OrcamentoService(IRepository<Orcamento> repository) : IOrcamentoService
{
    public async Task<IReadOnlyList<OrcamentoDto>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).OrderByDescending(x => x.CreatedAt).Select(Map).ToList();

    public async Task<OrcamentoDto> SaveAsync(OrcamentoDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Situacao))
            throw new ArgumentException("Código e situação são obrigatórios.");
        var code = dto.Codigo.Trim();
        var duplicate = (await repository.ListAsync(ct)).Any(x =>
            x.Id != dto.Id && x.Codigo.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
            throw new ArgumentException($"Já existe um orçamento com o código {code}. Informe outro código.");
        var entity = dto.Id is null ? new Orcamento { CreatedAt = DateTime.UtcNow } :
            await repository.GetByIdAsync(dto.Id.Value, ct) ?? throw new NotFoundException("Orçamento não encontrado.");
        entity.Codigo = code; entity.Nome = dto.Nome?.Trim(); entity.Descricao = dto.Descricao?.Trim();
        entity.Situacao = dto.Situacao.Trim(); entity.Telefone = dto.Telefone; entity.Email = dto.Email?.Trim();
        entity.Data = dto.Data ?? DateOnly.FromDateTime(DateTime.Today); entity.TipoServico = dto.TipoServico;
        entity.ValorTotal = dto.ValorTotal; entity.UpdatedAt = DateTime.UtcNow;
        if (dto.Id is null) await repository.AddAsync(entity, ct); else repository.Update(entity);
        await repository.SaveChangesAsync(ct); return Map(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Orçamento não encontrado.");
        repository.Remove(entity); await repository.SaveChangesAsync(ct);
    }
    private static OrcamentoDto Map(Orcamento x) => new(x.Id, x.Codigo, x.Nome, x.Descricao, x.Situacao, x.Telefone, x.TipoServico, x.ValorTotal, x.Data, x.Email);
}

public sealed record PrestadorDto(long? Id, [Required] string Nome, string? CnpjCpf, string? Telefone,
    string? Email, string? MetodoPagamento, string? ChavePix, string? Banco, string? Agencia, string? Conta);

public sealed record PrestadorServicoVinculadoDto(
    string? Codigo, string? Cliente, AcompanhamentoServicoTipo Tipo, string? Situacao,
    decimal? ValorContrato, decimal? ValorProvisionado, decimal? ValorEfetivo);

public sealed record PrestadorLancamentoVinculadoDto(
    DateOnly? Data, string? Descricao, decimal? Valor, string Tipo, string Situacao);

public sealed record PrestadorDetalheDto(
    PrestadorDto Prestador,
    IReadOnlyList<PrestadorServicoVinculadoDto> Servicos,
    IReadOnlyList<PrestadorLancamentoVinculadoDto> Lancamentos);

public interface IPrestadorService
{
    Task<IReadOnlyList<PrestadorDto>> ListAsync(CancellationToken ct = default);
    Task<PrestadorDetalheDto> GetDetalheAsync(long id, CancellationToken ct = default);
    Task<PrestadorDto> SaveAsync(PrestadorDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class PrestadorService(
    IRepository<Prestador> repository,
    IRepository<CadastroServico> servicos,
    IRepository<Lancamento> lancamentos) : IPrestadorService
{
    public async Task<IReadOnlyList<PrestadorDto>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).OrderBy(x => x.Nome).Select(Map).ToList();

    public async Task<PrestadorDetalheDto> GetDetalheAsync(long id, CancellationToken ct = default)
    {
        var prestador = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Prestador não encontrado.");

        var todosServicos = await servicos.ListAsync(ct);
        var servicosVinculados = todosServicos
            .Where(s => s.Prestadores.Any(p => p.PrestadorId == id))
            .Select(s =>
            {
                var vinculos = s.Prestadores.Where(p => p.PrestadorId == id);
                return new PrestadorServicoVinculadoDto(
                    s.Codigo,
                    s.Cliente?.RazaoSocial,
                    s.TipoServico,
                    s.SituacaoInicial,
                    s.ValorContrato,
                    vinculos.Sum(p => p.ValorProvisionado ?? 0m),
                    vinculos.Sum(p => p.ValorEfetivo ?? 0m));
            })
            .ToList();

        var todosLancamentos = await lancamentos.ListAsync(ct);
        var lancamentosVinculados = todosLancamentos
            .Where(l => l.PrestadorId == id)
            .OrderByDescending(l => l.Data ?? DateOnly.MinValue)
            .Select(l => new PrestadorLancamentoVinculadoDto(
                l.Data,
                l.Descricao,
                l.Valor,
                l.Tipo.ToString(),
                l.Status.ToString()))
            .ToList();

        return new PrestadorDetalheDto(Map(prestador), servicosVinculados, lancamentosVinculados);
    }

    public async Task<PrestadorDto> SaveAsync(PrestadorDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome do prestador é obrigatório.");
        var entity = dto.Id is null ? new Prestador { CreatedAt = DateTime.UtcNow } :
            await repository.GetByIdAsync(dto.Id.Value, ct) ?? throw new NotFoundException("Prestador não encontrado.");
        entity.Nome=dto.Nome.Trim(); entity.CnpjCpf=dto.CnpjCpf; entity.Telefone=dto.Telefone; entity.Email=dto.Email;
        entity.MetodoPagamento=dto.MetodoPagamento; entity.ChavePix=dto.ChavePix; entity.Banco=dto.Banco;
        entity.Agencia=dto.Agencia; entity.Conta=dto.Conta; entity.UpdatedAt=DateTime.UtcNow;
        if(dto.Id is null) await repository.AddAsync(entity,ct); else repository.Update(entity);
        await repository.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteAsync(long id,CancellationToken ct=default)
    {
        var entity=await repository.GetByIdAsync(id,ct)??throw new NotFoundException("Prestador não encontrado.");
        repository.Remove(entity); await repository.SaveChangesAsync(ct);
    }
    private static PrestadorDto Map(Prestador x)=>new(x.Id,x.Nome??"",x.CnpjCpf,x.Telefone,x.Email,x.MetodoPagamento,x.ChavePix,x.Banco,x.Agencia,x.Conta);
}

public sealed record NotificationDto(long Id,string Titulo,string? Mensagem,NotificationCategory Categoria,
    DateTimeOffset CriadaEm,bool Lida,DateTimeOffset? ConfirmadaEm,string? Referencia);
public sealed record NotificationRuleDto(long? Id,string Nome,NotificationRuleType Tipo,NotificationCategory Categoria,
    int Dias,bool Ativa);
public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(long userId,CancellationToken ct=default);
    Task MarkReadAsync(long userId,long id,CancellationToken ct=default);
    Task ConfirmAsync(long userId,long id,CancellationToken ct=default);
    Task<IReadOnlyList<NotificationRuleDto>> ListRulesAsync(CancellationToken ct=default);
    Task<NotificationRuleDto> SaveRuleAsync(NotificationRuleDto dto,CancellationToken ct=default);
    Task<int> RunRulesAsync(long userId,CancellationToken ct=default);
}

public sealed class NotificationService(IRepository<Notification> repository,IRepository<NotificationRule> rules,
    IRepository<Lancamento> entries,IRepository<AcompanhamentoServico> tracking) : INotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(long userId, CancellationToken ct = default)
    {
        var items = (await repository.ListAsync(ct)).Where(x => x.UserId == userId).ToList();
        if (items.Count == 0)
        {
            var now = DateTimeOffset.UtcNow;
            var seed = new List<Notification>
            {
                new() { UserId = userId, Title = "📢 Sistema Atualizado para v2.4.0", Message = "O CRM Atlas foi atualizado com suporte a Auth0, emissão de PDF e design responsivo.", Category = NotificationCategory.TECNICA, ReferenceKey = "sys:welcome:v24", CreatedAt = now },
                new() { UserId = userId, Title = "📋 Central de Acompanhamento Operacional", Message = "Cadastros de AVCB, CLCB, Obras e Processos Administrativos estão sincronizados.", Category = NotificationCategory.TECNICA, ReferenceKey = "sys:welcome:op", CreatedAt = now.AddMinutes(-5) },
                new() { UserId = userId, Title = "💰 Controle Financeiro e Notas Fiscais", Message = "Gerencie entradas, saídas, NFs e condições de pagamento de forma centralizada.", Category = NotificationCategory.FINANCEIRA, ReferenceKey = "sys:welcome:fin", CreatedAt = now.AddMinutes(-10) }
            };
            foreach (var n in seed) await repository.AddAsync(n, ct);
            await repository.SaveChangesAsync(ct);
            items = seed;
        }
        return items.OrderByDescending(x => x.CreatedAt).Select(Map).ToList();
    }
    public async Task MarkReadAsync(long userId,long id,CancellationToken ct=default)
    {
        var item=await Owned(userId,id,ct);item.IsRead=true;repository.Update(item);await repository.SaveChangesAsync(ct);
    }
    public async Task ConfirmAsync(long userId,long id,CancellationToken ct=default)
    {
        var item=await Owned(userId,id,ct);item.IsRead=true;item.ConfirmedAt=DateTimeOffset.UtcNow;repository.Update(item);await repository.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<NotificationRuleDto>> ListRulesAsync(CancellationToken ct=default)=>
        (await rules.ListAsync(ct)).OrderBy(x=>x.Name).Select(MapRule).ToList();
    public async Task<NotificationRuleDto> SaveRuleAsync(NotificationRuleDto dto,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(dto.Nome)||dto.Dias<1)throw new ArgumentException("Nome e prazo maior que zero são obrigatórios.");
        var item=dto.Id is null?new NotificationRule{CreatedAt=DateTime.UtcNow}:await rules.GetByIdAsync(dto.Id.Value,ct)??throw new NotFoundException("Regra não encontrada.");
        item.Name=dto.Nome.Trim();item.Type=dto.Tipo;item.Category=dto.Categoria;item.DaysThreshold=dto.Dias;item.Enabled=dto.Ativa;item.UpdatedAt=DateTime.UtcNow;
        if(dto.Id is null)await rules.AddAsync(item,ct);else rules.Update(item);await rules.SaveChangesAsync(ct);return MapRule(item);
    }
    public async Task<int> RunRulesAsync(long userId,CancellationToken ct=default)
    {
        var active=(await rules.ListAsync(ct)).Where(x=>x.Enabled).ToList();var existing=await repository.ListAsync(ct);
        var now=DateTimeOffset.UtcNow;var count=0;
        foreach(var rule in active)
        {
            if(rule.Type==NotificationRuleType.PARCELA_A_VENCER)
            {
                var limit=DateOnly.FromDateTime(DateTime.Today.AddDays(rule.DaysThreshold));
                foreach(var item in (await entries.ListAsync(ct)).Where(x=>x.Status!=LancamentoStatus.PAGO&&x.DataPrevistaParcela is not null&&x.DataPrevistaParcela<=limit))
                    count+=await AddIfMissing(userId,$"parcela:{item.Id}:{rule.Id}",$"Parcela a vencer — {item.Codigo}",item.Descricao,rule,existing,now,ct);
            }
            else
            {
                var limit=DateTime.UtcNow.AddDays(-rule.DaysThreshold);
                foreach(var item in (await tracking.ListAsync(ct)).Where(x=>x.UpdatedAt<=limit))
                    count+=await AddIfMissing(userId,$"servico:{item.Id}:{rule.Id}",$"Serviço sem atualização — {item.Codigo}",item.NomeCliente,rule,existing,now,ct);
            }
        }
        if(count>0)await repository.SaveChangesAsync(ct);return count;
    }
    private async Task<int> AddIfMissing(long userId,string key,string title,string? message,NotificationRule rule,
        IReadOnlyList<Notification> existing,DateTimeOffset now,CancellationToken ct)
    {
        if(existing.Any(x=>x.UserId==userId&&x.ReferenceKey==key))return 0;
        await repository.AddAsync(new(){UserId=userId,Title=title,Message=message,Category=rule.Category,RuleType=rule.Type,
            ReferenceKey=key,CreatedAt=now,LastActive=now},ct);return 1;
    }
    private async Task<Notification> Owned(long userId,long id,CancellationToken ct)
    {
        var item=await repository.GetByIdAsync(id,ct)??throw new NotFoundException("Notificação não encontrada.");
        if(item.UserId!=userId)throw new UnauthorizedAccessException("Notificação não pertence ao usuário.");
        return item;
    }
    private static NotificationDto Map(Notification x)=>new(x.Id,x.Title,x.Message,x.Category,x.CreatedAt,x.IsRead,x.ConfirmedAt,x.ReferenceKey);
    private static NotificationRuleDto MapRule(NotificationRule x)=>new(x.Id,x.Name,x.Type,x.Category,x.DaysThreshold,x.Enabled);
}
