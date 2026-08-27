using CrmAtlas.ApplicationCore.Dashboard;
using CrmAtlas.ApplicationCore.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CrmAtlas.Infrastructure.Data;

internal sealed class DashboardQueryService(AtlasDbContext db) : IDashboardQueryService
{
    // Rotulos amigaveis das linhas de servico. Os graficos do frontend pintam
    // cada linha por esse nome; quando a consulta devolvia o nome do enum
    // ("PROCESSOS_ADM"), o mapa de cores nao encontrava a chave e o grafico
    // saia todo na cor de fallback.
    private static readonly (string Name, AcompanhamentoServicoTipo Type)[] LineSpecs =
    [
        ("AVCB", AcompanhamentoServicoTipo.AVCB),
        ("CLCB", AcompanhamentoServicoTipo.CLCB),
        ("Proc. Adm", AcompanhamentoServicoTipo.PROCESSOS_ADM),
        ("Obras", AcompanhamentoServicoTipo.OBRAS)
    ];

    private static string LineLabel(AcompanhamentoServicoTipo type)
    {
        foreach (var spec in LineSpecs)
            if (spec.Type == type) return spec.Name;
        return type.ToString();
    }

    public async Task<DashboardSnapshot> GetAsync(
        DashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        var types = filter.ServiceTypes.Count == 0
            ? Enum.GetValues<AcompanhamentoServicoTipo>()
            : filter.ServiceTypes;

        var entries = await db.Lancamentos
            .AsNoTracking()
            .Where(x => x.Data >= filter.Start && x.Data <= filter.End)
            // The executive dashboard is cash-basis: forecasts and items still
            // awaiting confirmation are not revenue/cost until they are paid.
            .Where(x => x.Status == LancamentoStatus.PAGO)
            .Where(x => x.CadastroServico == null || types.Contains(x.CadastroServico.TipoServico))
            .Select(x => new
            {
                x.Codigo, x.Descricao, x.NomeCliente, x.Data, x.Tipo, x.Valor,
                ServiceType = x.CadastroServico == null
                    ? (AcompanhamentoServicoTipo?)null
                    : x.CadastroServico.TipoServico
            })
            .ToListAsync(cancellationToken);

        var indirectCostsQuery = db.CustosIndiretos
            .AsNoTracking()
            .Where(x => x.Data >= filter.Start && x.Data <= filter.End);

        if (!filter.IncludeProLabore)
        {
            indirectCostsQuery = indirectCostsQuery.Where(x =>
                !x.Categoria.ToLower().Contains("prolabore") &&
                !x.Categoria.ToLower().Contains("pró-labore"));
        }

        var indirectCosts = await indirectCostsQuery
            .Select(x => new { x.Data, x.Categoria, x.Valor })
            .ToListAsync(cancellationToken);

        var contractsQuery = db.CadastrosServico
            .AsNoTracking()
            .Where(x => x.DataContrato >= filter.Start && x.DataContrato <= filter.End)
            .Where(x => types.Contains(x.TipoServico));

        if (filter.MinContractValue.HasValue)
            contractsQuery = contractsQuery.Where(x => x.ValorContrato >= filter.MinContractValue.Value);
        if (filter.MaxContractValue.HasValue)
            contractsQuery = contractsQuery.Where(x => x.ValorContrato <= filter.MaxContractValue.Value);

        var contracts = await contractsQuery
            .Select(x => new { x.Codigo, x.RazaoSocialEmpresa, x.TipoServico, x.ValorContrato, x.DataContrato })
            .ToListAsync(cancellationToken);

        var periodDays = (filter.End.ToDateTime(TimeOnly.MinValue) - filter.Start.ToDateTime(TimeOnly.MinValue)).Days + 1;
        var prevStart = filter.Start.AddDays(-periodDays);
        var prevEnd = filter.Start.AddDays(-1);

        var prevContractsQuery = db.CadastrosServico
            .AsNoTracking()
            .Where(x => x.DataContrato >= prevStart && x.DataContrato <= prevEnd)
            .Where(x => types.Contains(x.TipoServico));

        if (filter.MinContractValue.HasValue)
            prevContractsQuery = prevContractsQuery.Where(x => x.ValorContrato >= filter.MinContractValue.Value);
        if (filter.MaxContractValue.HasValue)
            prevContractsQuery = prevContractsQuery.Where(x => x.ValorContrato <= filter.MaxContractValue.Value);

        var prevContracts = await prevContractsQuery
            .Select(x => new { x.TipoServico, x.ValorContrato })
            .ToListAsync(cancellationToken);

        var prioritiesQuery = db.Acompanhamentos
            .AsNoTracking()
            .Where(x => types.Contains(x.TipoServico));

        if (filter.MinContractValue.HasValue)
            prioritiesQuery = prioritiesQuery.Where(x => x.ValorContrato >= filter.MinContractValue.Value);
        if (filter.MaxContractValue.HasValue)
            prioritiesQuery = prioritiesQuery.Where(x => x.ValorContrato <= filter.MaxContractValue.Value);

        var prioritiesRaw = await prioritiesQuery
            .Select(x => new
            {
                x.Id, x.Codigo, x.NomeCliente, x.TipoServico, x.Situacao, x.Endereco,
                x.ValorContrato, x.Recebido, x.AReceber, x.DataContrato, x.UltimaMudancaSituacaoEm, x.CreatedAt,
                OpenPendencies = x.Pendencias.Count(p => !p.Concluida)
            })
            .ToListAsync(cancellationToken);

        // Cartao "A receber", ranking de clientes e grafico de recebiveis por
        // linha passam a sair da MESMA base: acompanhamentos com data de
        // contrato dentro do periodo. Antes o cartao filtrava por periodo no
        // banco e o grafico somava o historico inteiro, entao os dois numeros
        // nunca fechavam na mesma tela. A lista de prioridades continua sem
        // filtro de data, porque ali o objetivo e o que esta em aberto hoje.
        var trackingInPeriod = prioritiesRaw
            .Where(x => x.DataContrato.HasValue
                && x.DataContrato.Value >= filter.Start
                && x.DataContrato.Value <= filter.End)
            .ToList();

        var clientCount = await db.Clientes.AsNoTracking().CountAsync(cancellationToken);
        var serviceCount = await db.CadastrosServico.AsNoTracking()
            .CountAsync(x => types.Contains(x.TipoServico), cancellationToken);
        var receivable = trackingInPeriod.Sum(x => (x.ValorContrato ?? 0) - (x.Recebido ?? 0));

        var revenue = entries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor ?? 0);
        var directCosts = entries.Where(x => x.Tipo == LancamentoTipo.SAIDA).Sum(x => x.Valor ?? 0);
        var indirectTotal = indirectCosts.Sum(x => x.Valor);
        var result = revenue - directCosts - indirectTotal;
        var closedContractsValue = contracts.Sum(x => x.ValorContrato ?? 0);

        var periods = PeriodRange(filter.Start, filter.End, filter.Granularity);
        var periodPoints = periods.Select(period =>
        {
            var periodEntries = entries.Where(x => InPeriod(x.Data, period, filter.Granularity));
            var periodRevenue = periodEntries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor ?? 0);
            var periodDirect = periodEntries.Where(x => x.Tipo == LancamentoTipo.SAIDA).Sum(x => x.Valor ?? 0);
            var periodIndirect = indirectCosts
                .Where(x => InPeriod(x.Data, period, filter.Granularity))
                .Sum(x => x.Valor);
            return new DashboardPeriodPoint(
                PeriodLabel(period, filter.Granularity),
                periodRevenue, periodDirect, periodIndirect, periodRevenue - periodDirect - periodIndirect);
        }).ToList();

        var revenueByService = entries
            .Where(x => x.Tipo == LancamentoTipo.ENTRADA)
            .GroupBy(x => x.ServiceType.HasValue ? LineLabel(x.ServiceType.Value) : "Sem vínculo")
            .Select(x => new DashboardBreakdownPoint(x.Key, x.Sum(y => y.Valor ?? 0), x.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        // "Contrato todos os serviços": uma barra por linha ativa, sempre as
        // quatro, mesmo quando alguma nao teve contrato no periodo — assim o
        // grafico nao muda de forma a cada filtro e fica igual ao da planilha.
        var contractsByService = LineSpecs
            .Where(l => types.Contains(l.Type))
            .Select(l =>
            {
                var items = contracts.Where(x => x.TipoServico == l.Type).ToList();
                return new DashboardBreakdownPoint(l.Name, items.Sum(x => x.ValorContrato ?? 0), items.Count);
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var costBreakdown = indirectCosts
            .GroupBy(x => x.Categoria)
            .Select(x => new DashboardBreakdownPoint(x.Key, x.Sum(y => y.Valor), x.Count()))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        var topClients = trackingInPeriod
            .GroupBy(x => x.NomeCliente ?? "Cliente não informado")
            .Select((g, idx) =>
            {
                var primaryLine = g.GroupBy(x => x.TipoServico)
                    .OrderByDescending(x => x.Sum(y => y.ValorContrato ?? 0))
                    .Select(x => x.Key.ToString())
                    .FirstOrDefault() ?? "Geral";
                var codes = string.Join(", ", g.Select(x => x.Codigo).Where(c => !string.IsNullOrEmpty(c)).Take(4));
                return new DashboardTopClientItem(
                    idx + 1,
                    g.Key,
                    codes,
                    primaryLine,
                    g.Sum(x => x.ValorContrato ?? 0),
                    g.Count(),
                    g.Sum(x => (x.ValorContrato ?? 0) - (x.Recebido ?? 0)));
            })
            .OrderByDescending(x => x.TotalContracted)
            .Take(10)
            .Select((x, idx) => x with { Rank = idx + 1 })
            .ToList();

        var now = DateTime.UtcNow;
        var priorityList = prioritiesRaw
            .OrderByDescending(x => x.OpenPendencies)
            .ThenBy(x => x.DataContrato?.ToDateTime(TimeOnly.MinValue) ?? x.UltimaMudancaSituacaoEm ?? x.CreatedAt)
            .Select(x =>
            {
                var refDate = x.DataContrato?.ToDateTime(TimeOnly.MinValue) ?? x.UltimaMudancaSituacaoEm ?? x.CreatedAt;
                var days = Math.Max(0, (int)(now - refDate).TotalDays);
                return new DashboardPriorityItem(
                    x.Id,
                    x.Codigo,
                    x.NomeCliente ?? "Cliente não informado",
                    x.TipoServico,
                    x.Situacao ?? "Em andamento",
                    x.OpenPendencies,
                    days,
                    x.Endereco,
                    x.ValorContrato ?? 0,
                    (x.ValorContrato ?? 0) - (x.Recebido ?? 0));
            })
            .ToList();

        var rawMonthlyContracts = periods.Select(period =>
        {
            var periodContracts = contracts.Where(x => InPeriod(x.DataContrato, period, filter.Granularity)).ToList();
            var avcb = periodContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB).Sum(x => x.ValorContrato ?? 0);
            var clcb = periodContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.CLCB).Sum(x => x.ValorContrato ?? 0);
            var proc = periodContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.PROCESSOS_ADM).Sum(x => x.ValorContrato ?? 0);
            var obras = periodContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.OBRAS).Sum(x => x.ValorContrato ?? 0);
            var total = avcb + clcb + proc + obras;
            return new { Label = PeriodLabel(period, filter.Granularity), Avcb = avcb, Clcb = clcb, ProcAdm = proc, Obras = obras, Total = total };
        }).ToList();

        var monthlyContractPoints = rawMonthlyContracts.Select((m, idx) =>
        {
            decimal? ma3 = idx < 2 ? null : (rawMonthlyContracts[idx].Total + rawMonthlyContracts[idx - 1].Total + rawMonthlyContracts[idx - 2].Total) / 3m;
            return new DashboardMonthlyContractPoint(m.Label, m.Avcb, m.Clcb, m.ProcAdm, m.Obras, m.Total, ma3);
        }).ToList();

        var quantityComparisons = LineSpecs
            .Where(l => types.Contains(l.Type))
            .Select(l => new DashboardServiceQuantityComparison(
                l.Name,
                l.Type,
                contracts.Count(x => x.TipoServico == l.Type),
                prevContracts.Count(x => x.TipoServico == l.Type)))
            .ToList();

        var prevEntries = await db.Lancamentos
            .AsNoTracking()
            .Where(x => x.Data >= prevStart && x.Data <= prevEnd)
            .Where(x => x.Status == LancamentoStatus.PAGO)
            .Where(x => x.CadastroServico == null || types.Contains(x.CadastroServico.TipoServico))
            .Select(x => new { x.Data, x.Tipo, x.Valor })
            .ToListAsync(cancellationToken);

        var rawMonthlyRevenues = periods.Select(period =>
        {
            var mEntries = entries.Where(x => InPeriod(x.Data, period, filter.Granularity) && x.Tipo == LancamentoTipo.ENTRADA).ToList();
            var rev = mEntries.Sum(x => x.Valor ?? 0);
            var avcb = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.AVCB).Sum(x => x.Valor ?? 0);
            var clcb = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.CLCB).Sum(x => x.Valor ?? 0);
            var proc = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.PROCESSOS_ADM).Sum(x => x.Valor ?? 0);
            var obras = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.OBRAS).Sum(x => x.Valor ?? 0);

            var prevPeriodDate = period.AddDays(-periodDays);
            var prevPeriod = PeriodStart(prevPeriodDate, filter.Granularity);
            var prevRev = prevEntries
                .Where(x => x.Data.HasValue && PeriodStart(x.Data.Value, filter.Granularity) == prevPeriod && x.Tipo == LancamentoTipo.ENTRADA)
                .Sum(x => x.Valor ?? 0);

            return new { Label = PeriodLabel(period, filter.Granularity), Revenue = rev, PrevRevenue = prevRev, Avcb = avcb, Clcb = clcb, ProcAdm = proc, Obras = obras };
        }).ToList();

        var monthlyRevenues = rawMonthlyRevenues.Select((m, idx) =>
        {
            decimal? ma3 = idx < 2 ? null : (rawMonthlyRevenues[idx].Revenue + rawMonthlyRevenues[idx - 1].Revenue + rawMonthlyRevenues[idx - 2].Revenue) / 3m;
            return new DashboardMonthlyRevenuePoint(m.Label, m.Revenue, m.PrevRevenue, ma3, m.Avcb, m.Clcb, m.ProcAdm, m.Obras);
        }).ToList();

        var top5Categories = indirectCosts
            .GroupBy(x => x.Categoria)
            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Valor) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .Select(x => x.Category)
            .ToList();

        var monthlyIndirectCosts = periods.Select(period =>
        {
            var mCosts = indirectCosts
                .Where(x => InPeriod(x.Data, period, filter.Granularity))
                .ToList();

            var catList = new List<DashboardMonthlyIndirectCostCategory>();

            foreach (var cat in top5Categories)
            {
                var val = mCosts.Where(x => x.Categoria == cat).Sum(x => x.Valor);
                catList.Add(new DashboardMonthlyIndirectCostCategory(cat, val));
            }

            var outrasVal = mCosts.Where(x => !top5Categories.Contains(x.Categoria)).Sum(x => x.Valor);
            catList.Add(new DashboardMonthlyIndirectCostCategory("Outras", outrasVal));

            return new DashboardMonthlyIndirectCostPoint(
                PeriodLabel(period, filter.Granularity),
                catList);
        }).ToList();

        var receivableByService = LineSpecs
            .Where(l => types.Contains(l.Type))
            .Select(l =>
            {
                var lineItems = trackingInPeriod.Where(x => x.TipoServico == l.Type).ToList();
                return new DashboardBreakdownPoint(l.Name, lineItems.Sum(x => (x.ValorContrato ?? 0) - (x.Recebido ?? 0)), lineItems.Count);
            })
            .ToList();

        var revenuePrevious = prevEntries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor ?? 0);
        var closedContractsValuePrevious = prevContracts.Sum(x => x.ValorContrato ?? 0);
        var closedContractsCount = contracts.Count;

        return new DashboardSnapshot(
            new DashboardKpis(
                revenue, revenuePrevious, directCosts, indirectTotal, result,
                revenue == 0 ? 0 : result / revenue,
                receivable, closedContractsValue, closedContractsValuePrevious, closedContractsCount,
                clientCount, serviceCount, priorityList.Sum(x => x.OpenPendencies)),
            periodPoints,
            revenueByService,
            contractsByService,
            costBreakdown,
            topClients,
            priorityList,
            entries.OrderByDescending(x => x.Data)
                .Take(8)
                .Select(x => new DashboardRecentEntry(
                    x.Codigo, x.Descricao ?? "Lançamento", x.NomeCliente, x.Data ?? default,
                    x.Tipo == LancamentoTipo.ENTRADA, x.Valor ?? 0))
                .ToList(),
            monthlyContractPoints,
            quantityComparisons,
            monthlyRevenues,
            monthlyIndirectCosts,
            receivableByService);
    }

    // O preset "Tudo" do dashboard vai de 01/01/2020 ate hoje — mais de 70
    // meses. Com o teto antigo de 36 os graficos de serie temporal paravam em
    // dezembro/2022 enquanto os cartoes de KPI somavam o periodo inteiro, e as
    // duas leituras da mesma tela nao fechavam. 120 periodos cobrem 10 anos e
    // continuam limitando o tamanho da resposta.
    private const int MaxPeriods = 120;

    private static DateOnly PeriodStart(DateOnly d, DashboardGranularity g)
    {
        return g switch
        {
            DashboardGranularity.Semana => d.AddDays(-((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7),
            DashboardGranularity.Mes => new DateOnly(d.Year, d.Month, 1),
            DashboardGranularity.Trimestre => new DateOnly(d.Year, ((d.Month - 1) / 3) * 3 + 1, 1),
            DashboardGranularity.Ano => new DateOnly(d.Year, 1, 1),
            _ => d
        };
    }

    private static DateOnly PeriodNext(DateOnly period, DashboardGranularity g)
    {
        return g switch
        {
            DashboardGranularity.Semana => period.AddDays(7),
            DashboardGranularity.Mes => period.AddMonths(1),
            DashboardGranularity.Trimestre => period.AddMonths(3),
            DashboardGranularity.Ano => period.AddYears(1),
            _ => period
        };
    }

    private static string PeriodLabel(DateOnly period, DashboardGranularity g)
    {
        var ci = new System.Globalization.CultureInfo("pt-BR");
        return g switch
        {
            DashboardGranularity.Semana => $"Sem {ISOWeek.GetWeekOfYear(period.ToDateTime(TimeOnly.MinValue))}/{ISOWeek.GetYear(period.ToDateTime(TimeOnly.MinValue))}",
            DashboardGranularity.Mes => $"{ci.DateTimeFormat.AbbreviatedMonthNames[period.Month - 1].TrimEnd('.').ToLowerInvariant()}./{period.Year % 100:00}",
            DashboardGranularity.Trimestre => $"{((period.Month - 1) / 3) + 1}º tri/{period.Year % 100:00}",
            DashboardGranularity.Ano => period.Year.ToString(),
            _ => $"{ci.DateTimeFormat.AbbreviatedMonthNames[period.Month - 1].TrimEnd('.').ToLowerInvariant()}./{period.Year % 100:00}"
        };
    }

    private static bool InPeriod(DateOnly? date, DateOnly period, DashboardGranularity g)
    {
        return date.HasValue && PeriodStart(date.Value, g) == period;
    }

    private static IReadOnlyList<DateOnly> PeriodRange(DateOnly start, DateOnly end, DashboardGranularity g)
    {
        var first = PeriodStart(start, g);
        var last = PeriodStart(end, g);
        var result = new List<DateOnly>();
        for (var period = first; period <= last && result.Count < MaxPeriods; period = PeriodNext(period, g))
            result.Add(period);
        return result;
    }
}
