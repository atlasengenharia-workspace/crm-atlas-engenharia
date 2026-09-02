namespace CrmAtlas.ApplicationCore.IA;

public interface IAtlasAiService
{
    IAsyncEnumerable<string> AskAsync(string question, CancellationToken cancellationToken = default);
    Task<string> AskNonStreamingAsync(string question, CancellationToken cancellationToken = default);
}

public sealed record AtlasAiMessage(string Role, string Content);
