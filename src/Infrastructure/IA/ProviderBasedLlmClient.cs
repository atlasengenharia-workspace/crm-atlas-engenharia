using System.Runtime.CompilerServices;
using CrmAtlas.ApplicationCore.IA;
using Microsoft.Extensions.Options;

namespace CrmAtlas.Infrastructure.IA;

public sealed class ProviderBasedLlmClient(IServiceProvider services, IOptions<AtlasAiOptions> options) : ILlmClient
{
    private ILlmClient ResolveClient()
    {
        var provider = options.Value.Provider?.ToLowerInvariant() switch
        {
            "openai" => (ILlmClient?)services.GetService(typeof(OpenAiLlmClient)),
            "huggingface" => (ILlmClient?)services.GetService(typeof(HuggingFaceLlmClient)),
            "n8n" => (ILlmClient?)services.GetService(typeof(N8nLlmClient)),
            _ => (ILlmClient?)services.GetService(typeof(OpenAiLlmClient))
        };

        return provider ?? new FallbackLlmClient();
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = ResolveClient();
        await foreach (var chunk in client.CompleteStreamingAsync(messages, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<string> CompleteAsync(IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken = default)
    {
        var client = ResolveClient();
        return await client.CompleteAsync(messages, cancellationToken);
    }
}
