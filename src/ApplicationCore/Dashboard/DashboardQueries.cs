using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Dashboard;

public sealed record DashboardFilter(DateOnly Start, DateOnly End, IReadOnlyList<AcompanhamentoServicoTipo> ServiceTypes);

public sealed record DashboardKpis(
    decimal Revenue,
    decimal DirectCosts,
    decimal IndirectCosts,
    decimal Result,
    decimal Margin,
    decimal Receivable,
    int Clients,
    int Services,
    int OpenPendencies);

public sealed record DashboardPeriodPoint(
    string Label,
    decimal Revenue,
    decimal DirectCosts,
    decimal IndirectCosts,
    decimal Result);

public sealed record DashboardBreakdownPoint(string Label, decimal Value, int Count);

public sealed record DashboardPriorityItem(
    long Id,
    string Code,
    string Client,
    AcompanhamentoServicoTipo ServiceType,
    string Status,
    int OpenPendencies,
    int DaysInStatus);

public sealed record DashboardRecentEntry(
    string Code,
    string Description,
    string? Client,
    DateOnly Date,
    bool IsIncome,
    decimal Value);

public sealed record DashboardSnapshot(
    DashboardKpis Kpis,
    IReadOnlyList<DashboardPeriodPoint> Periods,
    IReadOnlyList<DashboardBreakdownPoint> RevenueByService,
    IReadOnlyList<DashboardBreakdownPoint> ContractsByService,
    IReadOnlyList<DashboardBreakdownPoint> IndirectCostsByCategory,
    IReadOnlyList<DashboardPriorityItem> Priorities,
    IReadOnlyList<DashboardRecentEntry> RecentEntries);

public interface IDashboardQueryService
{
    Task<DashboardSnapshot> GetAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
}
