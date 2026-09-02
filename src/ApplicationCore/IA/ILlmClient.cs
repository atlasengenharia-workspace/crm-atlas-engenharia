namespace CrmAtlas.ApplicationCore.IA;

public interface ILlmClient
{
    IAsyncEnumerable<string> CompleteStreamingAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        CancellationToken cancellationToken = default);

    Task<string> CompleteAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        CancellationToken cancellationToken = default);
}
