namespace CrmAtlas.ApplicationCore.Common;

/// <summary>
/// Situações que encerram o serviço. A comparação é exata (após normalização)
/// para não ocultar variações como "Concluído - Aguardando pagamento".
/// </summary>
public static class SituacaoFinalizada
{
    public static readonly string[] Nomes = ["concluído", "concluido", "finalizado", "encerrado"];

    public static bool Is(string? situacao) =>
        situacao is not null && Nomes.Contains(Normalizar(situacao), StringComparer.Ordinal);

    public static string Normalizar(string situacao) =>
        situacao.Trim().TrimEnd('.', ';', ':').ToLowerInvariant();
}
