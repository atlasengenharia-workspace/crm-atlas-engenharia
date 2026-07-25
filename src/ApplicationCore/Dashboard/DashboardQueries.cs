using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Dashboard;

public sealed record DashboardFilter(
    DateOnly Start,
    DateOnly End,
    IReadOnlyList<AcompanhamentoServicoTipo> ServiceTypes,
    bool IncludeProLabore = true,
    decimal? MinContractValue = null,
    decimal? MaxContractValue = null);

public sealed record DashboardKpis(
    decimal Revenue,
    decimal RevenuePrevious,
    decimal DirectCosts,
    decimal IndirectCosts,
    decimal Result,
    decimal Margin,
    decimal Receivable,
    decimal ClosedContractsValue,
    decimal ClosedContractsValuePrevious,
    int ClosedContractsCount,
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

public sealed record DashboardMonthlyContractPoint(
    string Label,
    decimal Avcb,
    decimal Clcb,
    decimal ProcAdm,
    decimal Obras,
    decimal Total,
    decimal? MovingAverage3);

public sealed record DashboardMonthlyRevenuePoint(
    string Label,
    decimal Revenue,
    decimal PreviousRevenue,
    decimal? MovingAverage3,
    decimal Avcb,
    decimal Clcb,
    decimal ProcAdm,
    decimal Obras);

public sealed record DashboardMonthlyIndirectCostCategory(
    string Category,
    decimal Value);

public sealed record DashboardMonthlyIndirectCostPoint(
    string MonthLabel,
    IReadOnlyList<DashboardMonthlyIndirectCostCategory> Categories);

public sealed record DashboardServiceQuantityComparison(
    string LineName,
    AcompanhamentoServicoTipo ServiceType,
    int CurrentCount,
    int PreviousCount);

public sealed record DashboardTopClientItem(
    int Rank,
    string ClientName,
    string ServiceCodes,
    string PrimaryLine,
    decimal TotalContracted,
    int ContractCount,
    decimal Receivable);

public sealed record DashboardPriorityItem(
    long Id,
    string Code,
    string Client,
    AcompanhamentoServicoTipo ServiceType,
    string Status,
    int OpenPendencies,
    int DaysInStatus,
    string? City = null,
    decimal ContractValue = 0,
    decimal ReceivableValue = 0);

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
    IReadOnlyList<DashboardTopClientItem> TopClients,
    IReadOnlyList<DashboardPriorityItem> Priorities,
    IReadOnlyList<DashboardRecentEntry> RecentEntries,
    IReadOnlyList<DashboardMonthlyContractPoint>? MonthlyContracts = null,
    IReadOnlyList<DashboardServiceQuantityComparison>? QuantityComparisons = null,
    IReadOnlyList<DashboardMonthlyRevenuePoint>? MonthlyRevenues = null,
    IReadOnlyList<DashboardMonthlyIndirectCostPoint>? MonthlyIndirectCosts = null,
    IReadOnlyList<DashboardBreakdownPoint>? ReceivableByService = null);

public interface IDashboardQueryService
{
    Task<DashboardSnapshot> GetAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
}
