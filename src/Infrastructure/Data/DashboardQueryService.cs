using CrmAtlas.ApplicationCore.Dashboard;
using CrmAtlas.ApplicationCore.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmAtlas.Infrastructure.Data;

internal sealed class DashboardQueryService(AtlasDbContext db) : IDashboardQueryService
{
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
                x.ValorContrato, x.AReceber, x.UltimaMudancaSituacaoEm, x.CreatedAt,
                OpenPendencies = x.Pendencias.Count(p => !p.Concluida)
            })
            .ToListAsync(cancellationToken);

        var clientCount = await db.Clientes.AsNoTracking().CountAsync(cancellationToken);
        var serviceCount = await db.CadastrosServico.AsNoTracking()
            .CountAsync(x => types.Contains(x.TipoServico), cancellationToken);
        var receivable = await db.Acompanhamentos.AsNoTracking()
            .Where(x => types.Contains(x.TipoServico))
            .SumAsync(x => x.AReceber ?? 0, cancellationToken);

        var revenue = entries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor ?? 0);
        var directCosts = entries.Where(x => x.Tipo == LancamentoTipo.SAIDA).Sum(x => x.Valor ?? 0);
        var indirectTotal = indirectCosts.Sum(x => x.Valor);
        var result = revenue - directCosts - indirectTotal;
        var closedContractsValue = contracts.Sum(x => x.ValorContrato ?? 0);

        var months = MonthRange(filter.Start, filter.End);
        var periods = months.Select(month =>
        {
            var monthEntries = entries.Where(x => x.Data?.Year == month.Year && x.Data?.Month == month.Month);
            var monthRevenue = monthEntries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor ?? 0);
            var monthDirect = monthEntries.Where(x => x.Tipo == LancamentoTipo.SAIDA).Sum(x => x.Valor ?? 0);
            var monthIndirect = indirectCosts
                .Where(x => x.Data.Year == month.Year && x.Data.Month == month.Month)
                .Sum(x => x.Valor);
            return new DashboardPeriodPoint(
                month.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                monthRevenue, monthDirect, monthIndirect, monthRevenue - monthDirect - monthIndirect);
        }).ToList();

        var revenueByService = entries
            .Where(x => x.Tipo == LancamentoTipo.ENTRADA)
            .GroupBy(x => x.ServiceType?.ToString() ?? "Sem vínculo")
            .Select(x => new DashboardBreakdownPoint(x.Key, x.Sum(y => y.Valor ?? 0), x.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        var contractsByService = contracts
            .GroupBy(x => x.TipoServico.ToString())
            .Select(x => new DashboardBreakdownPoint(x.Key, x.Sum(y => y.ValorContrato ?? 0), x.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        var costBreakdown = indirectCosts
            .GroupBy(x => x.Categoria)
            .Select(x => new DashboardBreakdownPoint(x.Key, x.Sum(y => y.Valor), x.Count()))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        var topClients = prioritiesRaw
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
                    g.Sum(x => x.AReceber ?? 0));
            })
            .OrderByDescending(x => x.TotalContracted)
            .Take(10)
            .Select((x, idx) => x with { Rank = idx + 1 })
            .ToList();

        var now = DateTime.UtcNow;
        var priorityList = prioritiesRaw
            .OrderByDescending(x => x.OpenPendencies)
            .ThenBy(x => x.UltimaMudancaSituacaoEm ?? x.CreatedAt)
            .Select(x =>
            {
                var refDate = x.UltimaMudancaSituacaoEm ?? x.CreatedAt;
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
                    x.AReceber ?? 0);
            })
            .ToList();

        var rawMonthlyContracts = months.Select(month =>
        {
            var monthContracts = contracts.Where(x => x.DataContrato?.Year == month.Year && x.DataContrato?.Month == month.Month).ToList();
            var avcb = monthContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB).Sum(x => x.ValorContrato ?? 0);
            var clcb = monthContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.CLCB).Sum(x => x.ValorContrato ?? 0);
            var proc = monthContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.PROCESSOS_ADM).Sum(x => x.ValorContrato ?? 0);
            var obras = monthContracts.Where(x => x.TipoServico == AcompanhamentoServicoTipo.OBRAS).Sum(x => x.ValorContrato ?? 0);
            var total = avcb + clcb + proc + obras;
            return new { Label = month.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")), Avcb = avcb, Clcb = clcb, ProcAdm = proc, Obras = obras, Total = total };
        }).ToList();

        var monthlyContractPoints = rawMonthlyContracts.Select((m, idx) =>
        {
            decimal? ma3 = idx < 2 ? null : (rawMonthlyContracts[idx].Total + rawMonthlyContracts[idx - 1].Total + rawMonthlyContracts[idx - 2].Total) / 3m;
            return new DashboardMonthlyContractPoint(m.Label, m.Avcb, m.Clcb, m.ProcAdm, m.Obras, m.Total, ma3);
        }).ToList();

        var lineSpecs = new[]
        {
            (Name: "AVCB", Type: AcompanhamentoServicoTipo.AVCB),
            (Name: "CLCB", Type: AcompanhamentoServicoTipo.CLCB),
            (Name: "Proc. Adm", Type: AcompanhamentoServicoTipo.PROCESSOS_ADM),
            (Name: "Obras", Type: AcompanhamentoServicoTipo.OBRAS)
        };

        var quantityComparisons = lineSpecs
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
            .Where(x => x.CadastroServico == null || types.Contains(x.CadastroServico.TipoServico))
            .Select(x => new { x.Data, x.Tipo, x.Valor })
            .ToListAsync(cancellationToken);

        var rawMonthlyRevenues = months.Select(month =>
        {
            var mEntries = entries.Where(x => x.Data?.Year == month.Year && x.Data?.Month == month.Month && x.Tipo == LancamentoTipo.ENTRADA).ToList();
            var rev = mEntries.Sum(x => x.Valor ?? 0);
            var avcb = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.AVCB).Sum(x => x.Valor ?? 0);
            var clcb = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.CLCB).Sum(x => x.Valor ?? 0);
            var proc = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.PROCESSOS_ADM).Sum(x => x.Valor ?? 0);
            var obras = mEntries.Where(x => x.ServiceType == AcompanhamentoServicoTipo.OBRAS).Sum(x => x.Valor ?? 0);

            var prevMonthDate = month.AddDays(-periodDays);
            var prevRev = prevEntries
                .Where(x => x.Data?.Year == prevMonthDate.Year && x.Data?.Month == prevMonthDate.Month && x.Tipo == LancamentoTipo.ENTRADA)
                .Sum(x => x.Valor ?? 0);

            return new { Label = month.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")), Revenue = rev, PrevRevenue = prevRev, Avcb = avcb, Clcb = clcb, ProcAdm = proc, Obras = obras };
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

        var monthlyIndirectCosts = months.Select(month =>
        {
            var mCosts = indirectCosts
                .Where(x => x.Data.Year == month.Year && x.Data.Month == month.Month)
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
                month.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                catList);
        }).ToList();

        var receivableByService = lineSpecs
            .Where(l => types.Contains(l.Type))
            .Select(l =>
            {
                var lineItems = prioritiesRaw.Where(x => x.TipoServico == l.Type).ToList();
                return new DashboardBreakdownPoint(l.Name, lineItems.Sum(x => x.AReceber ?? 0), lineItems.Count);
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
            periods,
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

    private static IReadOnlyList<DateOnly> MonthRange(DateOnly start, DateOnly end)
    {
        var first = new DateOnly(start.Year, start.Month, 1);
        var last = new DateOnly(end.Year, end.Month, 1);
        var result = new List<DateOnly>();
        for (var month = first; month <= last && result.Count < 36; month = month.AddMonths(1))
            result.Add(month);
        return result;
    }
}
