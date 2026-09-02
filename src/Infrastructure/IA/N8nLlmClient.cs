using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CrmAtlas.ApplicationCore.IA;
using Microsoft.Extensions.Options;

namespace CrmAtlas.Infrastructure.IA;

public sealed class N8nLlmClient(HttpClient client, IOptions<AtlasAiOptions> options) : ILlmClient
{
    public IAsyncEnumerable<string> CompleteStreamingAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>();
        _ = StreamAsync(channel.Writer, messages, cancellationToken);
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    public async Task<string> CompleteAsync(IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken = default)
    {
        var endpoint = options.Value.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            return await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);

        try
        {
            var lastMessage = messages.LastOrDefault(m => m.Role == "user")?.Content
                ?? messages.LastOrDefault()?.Content
                ?? string.Empty;

            var request = new N8nAiRequest(lastMessage, messages);
            var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                return text.Trim().Trim('"');
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"n8n retornou erro {response.StatusCode}: {error}. Resposta local:\n\n" + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Não foi possível acessar o n8n ({ex.Message}). Resposta local:\n\n" + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);
        }
    }

    private async Task StreamAsync(ChannelWriter<string> writer, IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = options.Value.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                await StreamFallbackAsync(writer, messages, cancellationToken);
                return;
            }

            var lastMessage = messages.LastOrDefault(m => m.Role == "user")?.Content
                ?? messages.LastOrDefault()?.Content
                ?? string.Empty;

            var request = new N8nAiRequest(lastMessage, messages);
            var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                text = text.Trim().Trim('"');
                foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    await writer.WriteAsync(word + " ", cancellationToken);
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                await writer.WriteAsync($"n8n retornou erro {response.StatusCode}: {error}. Resposta local:\n\n", cancellationToken);
                await StreamFallbackAsync(writer, messages, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await writer.WriteAsync($"Não foi possível acessar o n8n ({ex.Message}). Resposta local:\n\n", cancellationToken);
            await StreamFallbackAsync(writer, messages, cancellationToken);
        }
        finally
        {
            writer.Complete();
        }
    }

    private static async Task StreamFallbackAsync(ChannelWriter<string> writer, IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken)
    {
        await foreach (var chunk in new FallbackLlmClient().CompleteStreamingAsync(messages, cancellationToken))
        {
            await writer.WriteAsync(chunk, cancellationToken);
        }
    }

    private sealed record N8nAiRequest(string Question, IReadOnlyList<AtlasAiMessage> Messages);
}
