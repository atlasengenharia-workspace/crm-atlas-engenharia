namespace CrmAtlas.ApplicationCore.Enums;

/// <summary>
/// Situações padrão por tipo de serviço, usadas como base das opções quando não
/// há configuração cadastrada — grid e cadastro precisam oferecer o mesmo conjunto.
/// </summary>
public static class AcompanhamentoSituacaoDefaults
{
    public static IReadOnlyList<string> For(AcompanhamentoServicoTipo tipo) =>
        tipo == AcompanhamentoServicoTipo.AVCB
            ? ["Aguar. Contratante", "Aguar. Documentos", "Comunicado", "Concluído", "Concluído - Aguar. Pag.", "Em análise", "Executar"]
            : ["Em análise", "Em andamento", "Aguardando cliente", "Aguardando documentos", "Aguardando vistoria", "Concluído", "Cancelado"];
}
