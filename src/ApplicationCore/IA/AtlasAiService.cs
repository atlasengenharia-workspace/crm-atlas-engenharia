using System.Runtime.CompilerServices;

namespace CrmAtlas.ApplicationCore.IA;

public sealed class AtlasAiService(ILlmClient llmClient, IContextRetriever retriever) : IAtlasAiService
{
    private const string SystemPrompt = "Voce e Atlas, assistente operacional do CRM Atlas Engenharia. Responda em portugues do Brasil, de forma direta e objetiva. Use apenas as informacoes do contexto fornecido. Nao invente dados. Quando nao souber, diga que precisa de mais informacoes. Priorize acoes praticas: listar servicos pendentes, resumir financeiro, identificar orcamentos sem retorno, proximas vistorias.";

    public async IAsyncEnumerable<string> AskAsync(
        string question,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = await retriever.RetrieveAsync(question, cancellationToken);
        var messages = BuildMessages(question, context);
        await foreach (var chunk in llmClient.CompleteStreamingAsync(messages, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<string> AskNonStreamingAsync(string question, CancellationToken cancellationToken = default)
    {
        var context = await retriever.RetrieveAsync(question, cancellationToken);
        var messages = BuildMessages(question, context);
        return await llmClient.CompleteAsync(messages, cancellationToken);
    }

    private static IReadOnlyList<AtlasAiMessage> BuildMessages(string question, string context) =>
    [
        new AtlasAiMessage("system", SystemPrompt),
        new AtlasAiMessage("user", "Contexto do CRM:\n" + context + "\n\nPergunta do usuario:\n" + question),
        new AtlasAiMessage("assistant", "Entendido. Vou responder com base apenas no contexto fornecido.")
    ];
}
