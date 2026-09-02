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
    DateOnly? Data = null, [EmailAddress] string? Email = null,
    long? ServicoConvertidoId = null, string? ServicoConvertidoCodigo = null, DateTime? ConvertidoEm = null,
    string? Subtipo = null);

public sealed record OrcamentoFilter(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false,
    bool OcultarConcluidos = false);

public interface IOrcamentoService
{
    Task<PagedResult<OrcamentoDto>> ListAsync(OrcamentoFilter? filter = null, CancellationToken ct = default);
    Task<OrcamentoDto> SaveAsync(OrcamentoDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class OrcamentoService(
    IRepository<Orcamento> repository,
    IRepository<OrcamentoHistorico> historico,
    IUserAccessor userAccessor) : IOrcamentoService
{
    public async Task<PagedResult<OrcamentoDto>> ListAsync(OrcamentoFilter? filter = null, CancellationToken ct = default)
    {
        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                x.Codigo.Contains(term) ||
                (x.Nome != null && x.Nome.Contains(term)) ||
                (x.Descricao != null && x.Descricao.Contains(term)) ||
                (x.Telefone != null && x.Telefone.Contains(term)) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.Subtipo != null && x.Subtipo.Contains(term)));
        }

        if (filter?.OcultarConcluidos == true)
            query = query.Where(x => !Closed(x.Situacao));

        query = ApplySort(query, filter?.SortKey, filter?.SortDescending ?? false);

        var all = filter?.PageSize == 0;
        var pageSize = all ? 0 : CursorPagination.ClampPageSize(filter?.PageSize ?? 20);
        var page = Math.Max(1, filter?.Page ?? 1);
        var total = await repository.CountAsync(query, ct);
        var items = all
            ? await repository.ToListAsync(query, ct)
            : await repository.ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), ct);
        var dtos = items.Select(Map).ToList();

        return PagedResult<OrcamentoDto>.Create(dtos, page, all ? total : pageSize, total);
    }

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
        var responsavel = await userAccessor.GetUserNameAsync(ct);
        if (dto.Id is not null)
        {
            await TrackChangesAsync(entity, dto, responsavel, ct);
        }
        else
        {
            await historico.AddAsync(new OrcamentoHistorico
            {
                Orcamento = entity,
                Tipo = "Criacao",
                ValorNovo = $"{entity.Codigo} | {entity.Nome}",
                Responsavel = responsavel,
                AlteradoEm = DateTime.UtcNow
            }, ct);
        }
        entity.Codigo = code; entity.Nome = dto.Nome?.Trim(); entity.Descricao = dto.Descricao?.Trim();
        entity.Situacao = dto.Situacao.Trim(); entity.Telefone = dto.Telefone; entity.Email = dto.Email?.Trim();
        entity.Data = dto.Data ?? DateOnly.FromDateTime(DateTime.Today); entity.TipoServico = dto.TipoServico;
        entity.Subtipo = dto.Subtipo; entity.ValorTotal = dto.ValorTotal; entity.UpdatedAt = DateTime.UtcNow;
        if (dto.Id is null) await repository.AddAsync(entity, ct); else repository.Update(entity);
        await repository.SaveChangesAsync(ct); return Map(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Orçamento não encontrado.");
        repository.Remove(entity); await repository.SaveChangesAsync(ct);
    }

    private static bool Closed(string? s) => s is not null &&
        (s.Contains("aprov", StringComparison.OrdinalIgnoreCase) || s.Contains("recus", StringComparison.OrdinalIgnoreCase));

    private async Task TrackChangesAsync(Orcamento entity, OrcamentoDto dto, string? responsavel, CancellationToken ct)
    {
        async Task Add(string tipo, string? anterior, string? novo)
        {
            if (string.Equals(anterior, novo, StringComparison.Ordinal)) return;
            await historico.AddAsync(new OrcamentoHistorico
            {
                Orcamento = entity,
                Tipo = tipo,
                ValorAnterior = anterior,
                ValorNovo = novo,
                Responsavel = responsavel,
                AlteradoEm = DateTime.UtcNow
            }, ct);
        }

        await Task.WhenAll(
            Add("Codigo", entity.Codigo, dto.Codigo.Trim()),
            Add("Nome", entity.Nome, dto.Nome?.Trim()),
            Add("Descricao", entity.Descricao, dto.Descricao?.Trim()),
            Add("Situacao", entity.Situacao, dto.Situacao.Trim()),
            Add("Telefone", entity.Telefone, dto.Telefone),
            Add("Email", entity.Email, dto.Email?.Trim()),
            Add("Data", entity.Data?.ToString("O"), (dto.Data ?? DateOnly.FromDateTime(DateTime.Today)).ToString("O")),
            Add("TipoServico", entity.TipoServico.ToString(), dto.TipoServico.ToString()),
            Add("Subtipo", entity.Subtipo, dto.Subtipo),
            Add("ValorTotal", entity.ValorTotal?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), dto.ValorTotal?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static IQueryable<Orcamento> ApplySort(IQueryable<Orcamento> query, string? sortKey, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortKey)) return query.OrderByDescending(x => x.Id);

        var ordered = sortKey.ToLowerInvariant() switch
        {
            "codigo" => descending ? query.OrderByDescending(x => x.Codigo) : query.OrderBy(x => x.Codigo),
            "cliente" => descending ? query.OrderByDescending(x => x.Nome) : query.OrderBy(x => x.Nome),
            "telefone" => descending ? query.OrderByDescending(x => x.Telefone) : query.OrderBy(x => x.Telefone),
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "data" => descending ? query.OrderByDescending(x => x.Data) : query.OrderBy(x => x.Data),
            "tipo" => descending ? query.OrderByDescending(x => x.TipoServico) : query.OrderBy(x => x.TipoServico),
            "situacao" => descending ? query.OrderByDescending(x => x.Situacao) : query.OrderBy(x => x.Situacao),
            "valor" => descending ? query.OrderByDescending(x => x.ValorTotal) : query.OrderBy(x => x.ValorTotal),
            _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
        };

        return ordered.ThenBy(x => x.Id);
    }

    private static OrcamentoDto Map(Orcamento x) => new(x.Id, x.Codigo, x.Nome, x.Descricao, x.Situacao, x.Telefone, x.TipoServico, x.ValorTotal, x.Data, x.Email,
        x.ServicoConvertidoId, x.ServicoConvertidoCodigo, x.ConvertidoEm, x.Subtipo);
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

public sealed record PrestadorFilter(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false);

public interface IPrestadorService
{
    Task<PagedResult<PrestadorDto>> ListAsync(PrestadorFilter? filter = null, CancellationToken ct = default);
    Task<PrestadorDetalheDto> GetDetalheAsync(long id, CancellationToken ct = default);
    Task<PrestadorDto> SaveAsync(PrestadorDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class PrestadorService(
    IRepository<Prestador> repository,
    IRepository<CadastroServico> servicos,
    IRepository<Lancamento> lancamentos,
    ICrmCache cache) : IPrestadorService
{
    private const string PrestadoresCacheKey = "prestadores:all";

    public async Task<PagedResult<PrestadorDto>> ListAsync(PrestadorFilter? filter = null, CancellationToken ct = default)
    {
        var cacheable = filter is null || (filter.PageSize == 0 && string.IsNullOrWhiteSpace(filter.Search));
        if (cacheable)
        {
            var cached = await cache.GetAsync<PagedResult<PrestadorDto>>(PrestadoresCacheKey, ct);
            if (cached is not null) return cached;
        }

        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                (x.Nome != null && x.Nome.Contains(term)) ||
                (x.CnpjCpf != null && x.CnpjCpf.Contains(term)) ||
                (x.Telefone != null && x.Telefone.Contains(term)) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.MetodoPagamento != null && x.MetodoPagamento.Contains(term)));
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

        var result = PagedResult<PrestadorDto>.Create(dtos, page, all ? total : pageSize, total);
        if (cacheable)
            await cache.SetAsync(PrestadoresCacheKey, result, TimeSpan.FromHours(1), ct);
        return result;
    }

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
        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(PrestadoresCacheKey, ct);
        return Map(entity);
    }
    public async Task DeleteAsync(long id,CancellationToken ct=default)
    {
        var entity=await repository.GetByIdAsync(id,ct)??throw new NotFoundException("Prestador não encontrado.");
        repository.Remove(entity); await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(PrestadoresCacheKey, ct);
    }

    private static IQueryable<Prestador> ApplySort(IQueryable<Prestador> query, string? sortKey, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortKey)) return query.OrderByDescending(x => x.Id);

        var ordered = sortKey.ToLowerInvariant() switch
        {
            "prestador" => descending ? query.OrderByDescending(x => x.Nome) : query.OrderBy(x => x.Nome),
            "documento" => descending ? query.OrderByDescending(x => x.CnpjCpf) : query.OrderBy(x => x.CnpjCpf),
            "telefone" => descending ? query.OrderByDescending(x => x.Telefone) : query.OrderBy(x => x.Telefone),
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "pagamento" => descending ? query.OrderByDescending(x => x.MetodoPagamento) : query.OrderBy(x => x.MetodoPagamento),
            _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
        };

        return ordered.ThenBy(x => x.Id);
    }

    private static PrestadorDto Map(Prestador x)=>new(x.Id,x.Nome??"",x.CnpjCpf,x.Telefone,x.Email,x.MetodoPagamento,x.ChavePix,x.Banco,x.Agencia,x.Conta);
}

public sealed record NotificationDto(long Id,string Titulo,string? Mensagem,NotificationCategory Categoria,
    DateTimeOffset CriadaEm,bool Lida,DateTimeOffset? ConfirmadaEm,string? Referencia);
public sealed record NotificationRuleDto(long? Id,string Nome,NotificationRuleType Tipo,NotificationCategory Categoria,
    int Dias,bool Ativa);
public sealed record NotificationFilter(
    long UserId,
    int Page = 1,
    int PageSize = 20,
    long? AfterId = null);

public interface INotificationService
{
    Task<CursorResult<NotificationDto>> ListAsync(NotificationFilter filter,CancellationToken ct=default);
    Task MarkReadAsync(long userId,long id,CancellationToken ct=default);
    Task ConfirmAsync(long userId,long id,CancellationToken ct=default);
    Task<IReadOnlyList<NotificationRuleDto>> ListRulesAsync(CancellationToken ct=default);
    Task<NotificationRuleDto> SaveRuleAsync(NotificationRuleDto dto,CancellationToken ct=default);
    Task<int> RunRulesAsync(long userId,CancellationToken ct=default);
}

public sealed class NotificationService(IRepository<Notification> repository,IRepository<NotificationRule> rules,
    IRepository<Lancamento> entries,IRepository<AcompanhamentoServico> tracking) : INotificationService
{
    public async Task<CursorResult<NotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct = default)
    {
        var query = repository.AsQueryable().Where(x => x.UserId == filter.UserId);

        if (filter.AfterId is not null)
            query = query.Where(x => x.Id < filter.AfterId);

        query = query.OrderByDescending(x => x.Id);

        var pageSize = CursorPagination.ClampPageSize(filter.PageSize);
        var items = await repository.ToListAsync(query.Take(pageSize + 1), ct);

        if (items.Count == 0 && filter.AfterId is null)
        {
            var now = DateTimeOffset.UtcNow;
            var seed = new List<Notification>
            {
                new() { UserId = filter.UserId, Title = "📢 Sistema Atualizado para v2.4.0", Message = "O CRM Atlas foi atualizado com suporte a Auth0, emissão de PDF e design responsivo.", Category = NotificationCategory.TECNICA, ReferenceKey = "sys:welcome:v24", CreatedAt = now },
                new() { UserId = filter.UserId, Title = "📋 Central de Acompanhamento Operacional", Message = "Cadastros de AVCB, CLCB, Obras e Processos Administrativos estão sincronizados.", Category = NotificationCategory.TECNICA, ReferenceKey = "sys:welcome:op", CreatedAt = now.AddMinutes(-5) },
                new() { UserId = filter.UserId, Title = "💰 Controle Financeiro e Notas Fiscais", Message = "Gerencie entradas, saídas, NFs e condições de pagamento de forma centralizada.", Category = NotificationCategory.FINANCEIRA, ReferenceKey = "sys:welcome:fin", CreatedAt = now.AddMinutes(-10) }
            };
            foreach (var n in seed) await repository.AddAsync(n, ct);
            await repository.SaveChangesAsync(ct);
            return new CursorResult<NotificationDto>(seed.Select(Map).ToList(), filter.Page, pageSize, null, false);
        }

        var hasNext = items.Count > pageSize;
        var nextCursor = hasNext ? items[pageSize - 1].Id : (long?)null;
        var dtos = items.Take(pageSize).Select(Map).ToList();

        return new CursorResult<NotificationDto>(dtos, filter.Page, pageSize, nextCursor, hasNext);
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
