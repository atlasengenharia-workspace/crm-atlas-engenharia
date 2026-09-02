using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CrmAtlas.ApplicationCore.IA;

namespace CrmAtlas.Infrastructure.IA;

public sealed partial class FallbackLlmClient : ILlmClient
{
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var full = await CompleteAsync(messages, cancellationToken);
        foreach (var word in full.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            await Task.Yield();
            yield return word + " ";
        }
    }

    public Task<string> CompleteAsync(IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken = default)
    {
        var user = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var context = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;

        var response = GenerateLocalResponse(user, context);
        return Task.FromResult(response);
    }

    private static string GenerateLocalResponse(string question, string context)
    {
        var lower = question.ToLowerInvariant();
        if (lower.Contains("financeir") || lower.Contains("fatur") || lower.Contains("saldo"))
        {
            return ExtractFinanceSummary(context);
        }

        if (lower.Contains("orcamento") || lower.Contains("proposta"))
        {
            return ExtractBudgetSummary(context);
        }

        if (lower.Contains("pendencia") || lower.Contains("atrasado") || lower.Contains("vistoria"))
        {
            return ExtractServiceSummary(context, onlyPending: true);
        }

        return ExtractServiceSummary(context, onlyPending: false);
    }

    private static string ExtractFinanceSummary(string context)
    {
        var income = ParseCurrency(Regex.Match(context, @"- Entradas: ([^\n]+)").Groups[1].Value);
        var expense = ParseCurrency(Regex.Match(context, @"- Saidas: ([^\n]+)").Groups[1].Value);
        var balance = income - expense;
        return $"Neste mês, o CRM registra {income:C2} em entradas, {expense:C2} em saídas e saldo de {balance:C2}.";
    }

    private static string ExtractBudgetSummary(string context)
    {
        var total = Regex.Match(context, @"- Total: (\d+)").Groups[1].Value;
        var open = Regex.Match(context, @"- Em aberto: (\d+)").Groups[1].Value;
        return $"Existem {total} orçamentos cadastrados; {open} ainda estão em aberto.";
    }

    private static string ExtractServiceSummary(string context, bool onlyPending)
    {
        var total = Regex.Match(context, @"- Total: (\d+)").Groups[1].Value;
        var pending = Regex.Match(context, @"- Com pendências: (\d+)").Groups[1].Value;

        if (onlyPending)
            return $"{pending} dos {total} serviços possuem pendências abertas. Veja a fila operacional para priorizar.";

        return $"O CRM acompanha {total} serviços; {pending} possuem pendências em aberto.";
    }

    private static decimal ParseCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var clean = value.Replace("R$", string.Empty).Replace("R$ ", string.Empty).Trim();
        if (decimal.TryParse(clean, NumberStyles.Currency, new CultureInfo("pt-BR"), out var result))
            return result;
        return 0;
    }
}
