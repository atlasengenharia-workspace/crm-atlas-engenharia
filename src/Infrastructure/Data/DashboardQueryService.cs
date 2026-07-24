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

        var indirectCosts = await db.CustosIndiretos
            .AsNoTracking()
            .Where(x => x.Data >= filter.Start && x.Data <= filter.End)
            .Select(x => new { x.Data, x.Categoria, x.Valor })
            .ToListAsync(cancellationToken);

        var contracts = await db.CadastrosServico
            .AsNoTracking()
            .Where(x => x.DataContrato >= filter.Start && x.DataContrato <= filter.End)
            .Where(x => types.Contains(x.TipoServico))
            .Select(x => new { x.TipoServico, x.ValorContrato })
            .ToListAsync(cancellationToken);

        var priorities = await db.Acompanhamentos
            .AsNoTracking()
            .Where(x => types.Contains(x.TipoServico))
            .Select(x => new
            {
                x.Id, x.Codigo, x.NomeCliente, x.TipoServico, x.Situacao,
                x.UltimaMudancaSituacaoEm,
                OpenPendencies = x.Pendencias.Count(p => !p.Concluida)
            })
            .OrderByDescending(x => x.OpenPendencies)
            .ThenBy(x => x.UltimaMudancaSituacaoEm)
            .Take(12)
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

        var now = DateTime.UtcNow;
        return new DashboardSnapshot(
            new DashboardKpis(
                revenue, directCosts, indirectTotal, result,
                revenue == 0 ? 0 : result / revenue,
                receivable, clientCount, serviceCount, priorities.Sum(x => x.OpenPendencies)),
            periods,
            revenueByService,
            contractsByService,
            costBreakdown,
            priorities.Select(x => new DashboardPriorityItem(
                x.Id, x.Codigo, x.NomeCliente ?? "Cliente não informado", x.TipoServico,
                x.Situacao, x.OpenPendencies,
                Math.Max(0, (int)(now - (x.UltimaMudancaSituacaoEm ?? now)).TotalDays))).ToList(),
            entries.OrderByDescending(x => x.Data)
                .Take(8)
                .Select(x => new DashboardRecentEntry(
                    x.Codigo, x.Descricao ?? "Lançamento", x.NomeCliente, x.Data ?? default,
                    x.Tipo == LancamentoTipo.ENTRADA, x.Valor ?? 0))
                .ToList());
    }

    private static IReadOnlyList<DateOnly> MonthRange(DateOnly start, DateOnly end)
    {
        var first = new DateOnly(start.Year, start.Month, 1);
        var last = new DateOnly(end.Year, end.Month, 1);
        var result = new List<DateOnly>();
        for (var month = first; month <= last && result.Count < 24; month = month.AddMonths(1))
            result.Add(month);
        return result;
    }
}
