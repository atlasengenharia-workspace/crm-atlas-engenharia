using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Operacao;

namespace CrmAtlas.ApplicationCore.IA;

public sealed class AtlasAiContextRetriever(
    IAcompanhamentoService acompanhamentoService,
    IOrcamentoService orcamentoService,
    ILancamentoService lancamentoService) : IContextRetriever
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public async Task<string> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var lower = question.ToLowerInvariant();
        var sb = new StringBuilder();

        if (ContainsAny(lower, ["financeiro", "faturamento", "receita", "despesa", "saldo", "entrada", "saida", "custo"]))
        {
            var start = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = DateOnly.FromDateTime(DateTime.Today);
            var entries = (await lancamentoService.ListAsync(new(null, null, start, end, null, null, 1, 100), cancellationToken)).Items;
            var income = entries.Where(x => x.Tipo == LancamentoTipo.ENTRADA).Sum(x => x.Valor);
            var expense = entries.Where(x => x.Tipo == LancamentoTipo.SAIDA).Sum(x => x.Valor);
            sb.AppendLine($"### Resumo financeiro deste mes ({start:MM/yyyy})");
            sb.AppendLine($"- Entradas: {income:C2}");
            sb.AppendLine($"- Saidas: {expense:C2}");
            sb.AppendLine($"- Saldo: {(income - expense):C2}");
            sb.AppendLine($"- Lancamentos: {entries.Count}");
        }

        if (ContainsAny(lower, ["orcamento", "proposta", "budget"]))
        {
            var budgets = (await orcamentoService.ListAsync(new(PageSize: 100), cancellationToken)).Items;
            var open = budgets.Where(x => !IsClosed(x.Situacao)).ToList();
            sb.AppendLine($"### Orçamentos");
            sb.AppendLine($"- Total: {budgets.Count}");
            sb.AppendLine($"- Em aberto: {open.Count}");
            foreach (var b in open.Take(10))
            {
                sb.AppendLine($"- [{b.Codigo}] {b.Nome ?? "Sem cliente"} | {b.Situacao} | {b.ValorTotal:C2}");
            }
        }

        if (ContainsAny(lower, ["servico", "acompanhamento", "pendencia", "vistoria", "obra", "avcb", "clcb"]))
        {
            var services = (await acompanhamentoService.ListAsync(new(PageSize: 100, HideCompleted: lower.Contains("concluido")), cancellationToken)).Items;
            var withPending = services.Where(x => x.Pendencias > x.Concluidas).ToList();
            sb.AppendLine($"### Serviços");
            sb.AppendLine($"- Total: {services.Count}");
            sb.AppendLine($"- Com pendências: {withPending.Count}");
            foreach (var s in withPending.Take(10))
            {
                sb.AppendLine($"- [{s.Codigo}] {s.Cliente ?? "Sem cliente"} | {s.Servico ?? s.Tipo.ToString()} | {s.Situacao} | Pendencias: {s.Pendencias - s.Concluidas}");
            }
        }

        if (sb.Length == 0)
        {
            var services = (await acompanhamentoService.ListAsync(new(PageSize: 20), cancellationToken)).Items;
            var withPending = services.Where(x => x.Pendencias > x.Concluidas).ToList();
            sb.AppendLine($"### Visão geral operacional");
            sb.AppendLine($"- Serviços: {services.Count}");
            sb.AppendLine($"- Com pendências: {withPending.Count}");
        }

        return sb.ToString();
    }

    private static bool ContainsAny(string text, IEnumerable<string> values) => values.Any(text.Contains);

    private static bool IsClosed(string? situacao) =>
        situacao is not null &&
        (situacao.Contains("aprov", StringComparison.OrdinalIgnoreCase) ||
         situacao.Contains("recus", StringComparison.OrdinalIgnoreCase) ||
         situacao.Contains("fech", StringComparison.OrdinalIgnoreCase) ||
         situacao.Contains("concluido", StringComparison.OrdinalIgnoreCase));
}
