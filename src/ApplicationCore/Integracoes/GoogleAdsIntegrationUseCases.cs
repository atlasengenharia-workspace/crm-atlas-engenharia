using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Servicos;

namespace CrmAtlas.ApplicationCore.Integracoes;

public interface IGoogleAdsIntegrationService
{
    Task<PagedResult<GoogleAdsIntegrationDto>> ListAsync(GoogleAdsIntegrationFilter? filter = null, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> CreateAsync(GoogleAdsIntegrationDto dto, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> UpdateAsync(long id, GoogleAdsIntegrationDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<string> GetAuthorizationUrlAsync(long id, string redirectUri, string state, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> SaveRefreshTokenAsync(long id, string code, string redirectUri, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> SyncAsync(long id, string actor, CancellationToken cancellationToken = default);
    Task<GoogleAdsIntegrationDto> TestAsync(long id, CancellationToken cancellationToken = default);
    Task<GoogleAdsDashboardSummary> GetDashboardSummaryAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsCampaignDto>> ListCampaignsAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsMetricDto>> ListMetricsAsync(long id, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsLeadDto>> ListLeadsAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class GoogleAdsIntegrationService(
    IRepository<GoogleAdsIntegration> repository,
    IRepository<GoogleAdsCampaign> campaignRepository,
    IRepository<GoogleAdsCampaignMetric> metricRepository,
    IRepository<GoogleAdsLead> leadRepository,
    IRepository<GoogleAdsIntegrationAudit> auditRepository,
    IGoogleAdsApiClient apiClient,
    IRepository<Orcamento> orcamentoRepository,
    ILancamentoService lancamentoService,
    IRepository<Lancamento> lancamentoRepository,
    IRepository<Cliente> clienteRepository)
    : IGoogleAdsIntegrationService
{
    public async Task<PagedResult<GoogleAdsIntegrationDto>> ListAsync(GoogleAdsIntegrationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
            query = query.Where(x => x.Name.Contains(filter.Search.Trim()) || x.Key.Contains(filter.Search.Trim()));

        query = ApplySort(query, filter?.SortKey, filter?.SortDescending ?? false);

        var all = filter?.PageSize == 0;
        var pageSize = all ? 0 : CursorPagination.ClampPageSize(filter?.PageSize ?? 20);
        var page = Math.Max(1, filter?.Page ?? 1);
        var total = await repository.CountAsync(query, cancellationToken);
        var items = all
            ? await repository.ToListAsync(query, cancellationToken)
            : await repository.ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);
        var dtos = items.Select(ToDto).ToList();

        return PagedResult<GoogleAdsIntegrationDto>.Create(dtos, page, all ? total : pageSize, total);
    }

    public async Task<GoogleAdsIntegrationDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<GoogleAdsIntegrationDto> CreateAsync(GoogleAdsIntegrationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new GoogleAdsIntegration();
        await ApplyAsync(entity, dto, null, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GoogleAdsIntegrationDto> UpdateAsync(long id, GoogleAdsIntegrationDto dto, CancellationToken cancellationToken = default)
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

    public async Task<string> GetAuthorizationUrlAsync(long id, string redirectUri, string state, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        var config = ToConfig(entity);
        return await apiClient.BuildAuthorizationUrlAsync(config, redirectUri, state, cancellationToken);
    }

    public async Task<GoogleAdsIntegrationDto> SaveRefreshTokenAsync(long id, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        var config = ToConfig(entity);
        var refreshToken = await apiClient.ExchangeCodeAsync(config, redirectUri, code, cancellationToken);
        entity.EncryptedRefreshToken = refreshToken;
        entity.Status = GoogleIntegrationStatus.CONNECTED;
        entity.ErrorMessage = null;
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GoogleAdsIntegrationDto> SyncAsync(long id, string actor, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(entity.EncryptedRefreshToken))
            throw new InvalidOperationException("A integração não está conectada.");

        var config = ToConfig(entity);
        try
        {
            await SyncCampaignsAsync(entity, config, cancellationToken);
            await SyncMetricsAsync(entity, config, cancellationToken);
            if (entity.ImportLeadsAsBudgets)
                await SyncLeadsAndConvertAsync(entity, config, cancellationToken);
            if (entity.CreateFinancialEntries)
                await CreateFinancialEntriesAsync(entity, cancellationToken);

            entity.LastSyncAt = DateTimeOffset.UtcNow;
            entity.Status = GoogleIntegrationStatus.CONNECTED;
            entity.ErrorMessage = null;
            await auditRepository.AddAsync(new GoogleAdsIntegrationAudit
            {
                IntegrationId = entity.Id,
                Integration = entity,
                Action = GoogleIntegrationAction.SYNC,
                ResultStatus = GoogleIntegrationStatus.CONNECTED,
                Actor = actor,
                CreatedAt = DateTimeOffset.UtcNow,
                Message = "Sincronização concluída com sucesso."
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            entity.Status = GoogleIntegrationStatus.ERROR;
            entity.ErrorMessage = ex.Message;
            await auditRepository.AddAsync(new GoogleAdsIntegrationAudit
            {
                IntegrationId = entity.Id,
                Integration = entity,
                Action = GoogleIntegrationAction.SYNC,
                ResultStatus = GoogleIntegrationStatus.ERROR,
                Actor = actor,
                CreatedAt = DateTimeOffset.UtcNow,
                Message = ex.Message
            }, cancellationToken);
            throw;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GoogleAdsIntegrationDto> TestAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        var config = ToConfig(entity);
        await apiClient.TestConnectionAsync(config, cancellationToken);
        return ToDto(entity);
    }

    public async Task<GoogleAdsDashboardSummary> GetDashboardSummaryAsync(long id, CancellationToken cancellationToken = default)
    {
        var metrics = await metricRepository.ToListAsync(
            metricRepository.AsQueryable().Where(x => x.IntegrationId == id), cancellationToken);

        if (metrics.Count == 0)
            return new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var totalCost = metrics.Sum(x => x.CostMicros) / 1_000_000m;
        var totalClicks = metrics.Sum(x => x.Clicks);
        var totalImpressions = metrics.Sum(x => x.Impressions);
        var totalConversions = metrics.Sum(x => x.Conversions);
        var cpl = totalConversions > 0 ? totalCost / (decimal)totalConversions : 0;
        var avgCtr = totalImpressions > 0 ? metrics.Average(x => x.Ctr) : 0;
        var avgCpm = totalImpressions > 0 ? (decimal)metrics.Average(x => x.Cpm) / 1_000_000m : 0;
        var avgCpc = totalClicks > 0 ? (decimal)metrics.Average(x => x.Cpc) / 1_000_000m : 0;
        var activeCampaigns = await campaignRepository.CountAsync(
            campaignRepository.AsQueryable().Where(x => x.IntegrationId == id && x.Status == "ENABLED"), cancellationToken);
        var totalLeads = await leadRepository.CountAsync(
            leadRepository.AsQueryable().Where(x => x.IntegrationId == id), cancellationToken);

        var pendingLeads = await leadRepository.CountAsync(
            leadRepository.AsQueryable().Where(x => x.IntegrationId == id && !x.ConvertedToBudget), cancellationToken);

        return new(totalCost, totalClicks, totalImpressions, totalConversions, cpl, avgCtr, avgCpm, avgCpc, activeCampaigns, totalLeads, pendingLeads);
    }

    public async Task<IReadOnlyList<GoogleAdsCampaignDto>> ListCampaignsAsync(long id, CancellationToken cancellationToken = default)
    {
        var items = await campaignRepository.ToListAsync(
            campaignRepository.AsQueryable().Where(x => x.IntegrationId == id).OrderByDescending(x => x.Id), cancellationToken);
        return items.Select(c => new GoogleAdsCampaignDto(
            c.Id, c.IntegrationId, c.ExternalCampaignId, c.Name, c.Status, c.Channel,
            c.StartDate, c.EndDate, c.BudgetAmountMicros / 1_000_000m,
            c.Integration.Leads.Count(l => l.CampaignId == c.Id),
            c.Integration.Metrics.Where(m => m.CampaignId == c.Id).Sum(m => m.CostMicros) / 1_000_000m,
            c.SyncedAt)).ToList();
    }

    public async Task<IReadOnlyList<GoogleAdsMetricDto>> ListMetricsAsync(long id, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        var items = await metricRepository.ToListAsync(
            metricRepository.AsQueryable()
                .Where(x => x.IntegrationId == id && x.Date >= start && x.Date <= end)
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.Campaign.Name), cancellationToken);
        return items.Select(m => new GoogleAdsMetricDto(
            m.Id, m.CampaignId, m.Campaign.Name, m.Date,
            m.CostMicros / 1_000_000m, m.Clicks, m.Impressions, m.Conversions,
            m.ConversionsValue, m.AllConversions, m.Ctr, m.Cpm / 1_000_000d,
            m.Cpc / 1_000_000d, m.SyncedAt)).ToList();
    }

    public async Task<IReadOnlyList<GoogleAdsLeadDto>> ListLeadsAsync(long id, CancellationToken cancellationToken = default)
    {
        var items = await leadRepository.ToListAsync(
            leadRepository.AsQueryable().Where(x => x.IntegrationId == id).OrderByDescending(x => x.SubmittedAt), cancellationToken);
        return items.Select(l => new GoogleAdsLeadDto(
            l.Id, l.IntegrationId, l.CampaignId, l.Campaign.Name, l.SubmittedAt,
            l.GclId, l.Name, l.Email, l.Phone, l.Message, l.ConvertedToBudget,
            l.OrcamentoId, l.ConvertedAt)).ToList();
    }

    private async Task SyncCampaignsAsync(GoogleAdsIntegration entity, GoogleAdsIntegrationConfig config, CancellationToken cancellationToken)
    {
        var data = await apiClient.GetCampaignsAsync(config, cancellationToken);
        var existing = entity.Campaigns.ToDictionary(x => x.ExternalCampaignId);
        foreach (var item in data)
        {
            if (existing.TryGetValue(item.ExternalId, out var campaign))
            {
                campaign.Name = item.Name;
                campaign.Status = item.Status;
                campaign.Channel = item.Channel;
                campaign.StartDate = item.StartDate;
                campaign.EndDate = item.EndDate;
                campaign.BudgetAmountMicros = item.BudgetAmountMicros;
                campaign.SyncedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                entity.Campaigns.Add(new GoogleAdsCampaign
                {
                    ExternalCampaignId = item.ExternalId,
                    Name = item.Name,
                    Status = item.Status,
                    Channel = item.Channel,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    BudgetAmountMicros = item.BudgetAmountMicros,
                    SyncedAt = DateTimeOffset.UtcNow
                });
            }
        }
    }

    private async Task SyncMetricsAsync(GoogleAdsIntegration entity, GoogleAdsIntegrationConfig config, CancellationToken cancellationToken)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-30);
        var data = await apiClient.GetMetricsAsync(config, start, end, cancellationToken);
        var existing = entity.Metrics.ToDictionary(x => (x.CampaignId, x.Date));

        foreach (var item in data)
        {
            var campaign = entity.Campaigns.FirstOrDefault(x => x.ExternalCampaignId == item.ExternalCampaignId);
            if (campaign is null) continue;

            if (existing.TryGetValue((campaign.Id, item.Date), out var metric))
            {
                metric.CostMicros = item.CostMicros;
                metric.Clicks = item.Clicks;
                metric.Impressions = item.Impressions;
                metric.Conversions = item.Conversions;
                metric.ConversionsValue = item.ConversionsValue;
                metric.AllConversions = item.AllConversions;
                metric.Ctr = item.Ctr;
                metric.Cpm = item.Cpm;
                metric.Cpc = item.Cpc;
                metric.SyncedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                entity.Metrics.Add(new GoogleAdsCampaignMetric
                {
                    Campaign = campaign,
                    CampaignId = campaign.Id,
                    Date = item.Date,
                    CostMicros = item.CostMicros,
                    Clicks = item.Clicks,
                    Impressions = item.Impressions,
                    Conversions = item.Conversions,
                    ConversionsValue = item.ConversionsValue,
                    AllConversions = item.AllConversions,
                    Ctr = item.Ctr,
                    Cpm = item.Cpm,
                    Cpc = item.Cpc,
                    SyncedAt = DateTimeOffset.UtcNow
                });
            }
        }
    }

    private async Task SyncLeadsAndConvertAsync(GoogleAdsIntegration entity, GoogleAdsIntegrationConfig config, CancellationToken cancellationToken)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-7);
        var data = await apiClient.GetLeadsAsync(config, start, end, cancellationToken);

        foreach (var item in data)
        {
            var campaign = entity.Campaigns.FirstOrDefault(x => x.ExternalCampaignId == item.ExternalCampaignId);
            if (campaign is null) continue;

            if (entity.Leads.Any(x => x.GclId == item.GclId && x.CampaignId == campaign.Id)) continue;

            var lead = new GoogleAdsLead
            {
                Campaign = campaign,
                CampaignId = campaign.Id,
                SubmittedAt = item.SubmittedAt,
                GclId = item.GclId,
                Name = item.Name,
                Email = item.Email,
                Phone = item.Phone,
                Message = item.Message
            };
            entity.Leads.Add(lead);

            if (!entity.ImportLeadsAsBudgets) continue;

            var cliente = await FindOrCreateClienteAsync(item, cancellationToken);
            var orcamento = new Orcamento
            {
                Codigo = $"ORC-ADS-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                Nome = item.Name,
                Telefone = item.Phone,
                Email = item.Email,
                Descricao = $"Lead Google Ads - {campaign.Name}. {item.Message}",
                TipoServico = AcompanhamentoServicoTipo.AVCB,
                Situacao = "Em análise",
                ValorTotal = 0,
                CreatedAt = DateTime.UtcNow,
                Data = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            await orcamentoRepository.AddAsync(orcamento, cancellationToken);
            lead.ConvertedToBudget = true;
            lead.OrcamentoId = orcamento.Id;
            lead.ConvertedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task CreateFinancialEntriesAsync(GoogleAdsIntegration entity, CancellationToken cancellationToken)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-30);
        var metrics = entity.Metrics
            .Where(x => x.Date >= start && x.Date <= end)
            .GroupBy(x => x.Date)
            .Select(g => new { Date = g.Key, Cost = g.Sum(m => m.CostMicros) / 1_000_000m })
            .Where(x => x.Cost > 0)
            .ToList();

        foreach (var day in metrics)
        {
            var existing = await lancamentoRepository.FindAsync(
                x => x.Descricao == $"Investimento Google Ads - {day.Date:dd/MM/yyyy}" &&
                     x.Data == day.Date &&
                     x.Origem == LancamentoOrigem.IMPORT_ATLAS, cancellationToken);

            if (existing is not null) continue;

            await lancamentoService.CreateAsync(new LancamentoDto(
                null, null, LancamentoTipo.SAIDA, LancamentoStatus.PAGO, LancamentoOrigem.IMPORT_ATLAS,
                null, null, null, null, null,
                $"Investimento Google Ads - {day.Date:dd/MM/yyyy}",
                day.Date, day.Cost, null, null, "Débito automático", null,
                "Google Ads", null, null, null, null,
                0, day.Cost, -day.Cost, null, null), cancellationToken);
        }
    }

    private async Task<Cliente?> FindOrCreateClienteAsync(GoogleAdsLeadData lead, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            var byEmail = await clienteRepository.FindAsync(x => x.Email == lead.Email, cancellationToken);
            if (byEmail is not null)
                return byEmail;
        }

        if (!string.IsNullOrWhiteSpace(lead.Phone))
        {
            var byPhone = await clienteRepository.FindAsync(x => x.Telefone == lead.Phone, cancellationToken);
            if (byPhone is not null)
                return byPhone;
        }

        var cliente = new Cliente
        {
            RazaoSocial = lead.Name ?? "Lead Google Ads",
            NomeContato = lead.Name,
            Email = lead.Email,
            Telefone = lead.Phone
        };
        await clienteRepository.AddAsync(cliente, cancellationToken);
        return cliente;
    }

    private async Task ApplyAsync(GoogleAdsIntegration entity, GoogleAdsIntegrationDto dto, long? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
            throw new ArgumentException("A chave é obrigatória.");
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("O nome é obrigatório.");

        var key = dto.Key.Trim();
        var all = await repository.ListAsync(cancellationToken);
        if (all.Any(x => x.Id != currentId && x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe uma integração com essa chave.");

        entity.Key = key;
        entity.Name = dto.Name.Trim();
        entity.Description = (dto.Description ?? string.Empty).Trim();
        entity.DeveloperToken = dto.DeveloperToken;
        entity.ClientId = dto.ClientId;
        entity.EncryptedClientSecret = dto.ClientSecret;
        entity.EncryptedRefreshToken = dto.RefreshToken;
        entity.LoginCustomerId = dto.LoginCustomerId;
        entity.AutoSync = dto.AutoSync;
        entity.SyncIntervalMin = Math.Max(15, dto.SyncIntervalMin);
        entity.ImportLeadsAsBudgets = dto.ImportLeadsAsBudgets;
        entity.CreateFinancialEntries = dto.CreateFinancialEntries;
        entity.Status = string.IsNullOrWhiteSpace(dto.RefreshToken) ? GoogleIntegrationStatus.PENDING : entity.Status;
    }

    private async Task<GoogleAdsIntegration> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Integração Google Ads não encontrada: {id}.");

    private static GoogleAdsIntegrationConfig ToConfig(GoogleAdsIntegration entity) =>
        new(entity.DeveloperToken ?? string.Empty,
            entity.ClientId ?? string.Empty,
            entity.EncryptedClientSecret ?? string.Empty,
            entity.EncryptedRefreshToken,
            entity.LoginCustomerId);

    private static GoogleAdsIntegrationDto ToDto(GoogleAdsIntegration x) =>
        new(x.Id, x.Key, x.Name, x.Description, x.Status, x.DeveloperToken,
            x.ClientId, x.EncryptedClientSecret, x.EncryptedRefreshToken,
            x.LoginCustomerId, x.AutoSync, x.SyncIntervalMin,
            x.ImportLeadsAsBudgets, x.CreateFinancialEntries,
            x.LastSyncAt, x.ErrorMessage);

    private static IQueryable<GoogleAdsIntegration> ApplySort(IQueryable<GoogleAdsIntegration> query, string? sortKey, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortKey)) return descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id);

        var ordered = sortKey.ToLowerInvariant() switch
        {
            "nome" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "chave" => descending ? query.OrderByDescending(x => x.Key) : query.OrderBy(x => x.Key),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
        };

        return ordered.ThenBy(x => x.Id);
    }
}
