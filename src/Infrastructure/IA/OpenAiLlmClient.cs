using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrmAtlas.ApplicationCore.IA;
using Microsoft.Extensions.Options;

namespace CrmAtlas.Infrastructure.IA;

public sealed class OpenAiLlmClient(IHttpClientFactory httpClientFactory, IOptions<AtlasAiOptions> options) : ILlmClient
{
    private const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        IReadOnlyList<AtlasAiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await foreach (var chunk in new FallbackLlmClient().CompleteStreamingAsync(messages, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        var request = BuildRequest(messages, stream: true);
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var orgId = options.Value.OrgId;
        if (!string.IsNullOrWhiteSpace(orgId))
            client.DefaultRequestHeaders.Add("OpenAI-Organization", orgId);

        var endpoint = options.Value.Endpoint ?? DefaultEndpoint;
        var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (true)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal)) continue;
            var data = line[6..].Trim();
            if (data == "[DONE]") break;

            var chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, JsonOptions);
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
                yield return delta;
        }
    }

    public async Task<string> CompleteAsync(IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);

        var request = BuildRequest(messages, stream: false);
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var orgId = options.Value.OrgId;
        if (!string.IsNullOrWhiteSpace(orgId))
            client.DefaultRequestHeaders.Add("OpenAI-Organization", orgId);

        var endpoint = options.Value.Endpoint ?? DefaultEndpoint;
        var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<OpenAiCompletionResponse>(JsonOptions, cancellationToken);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
               ?? "Não foi possível obter resposta do modelo.";
    }

    private OpenAiRequest BuildRequest(IReadOnlyList<AtlasAiMessage> messages, bool stream) =>
        new(
            options.Value.Model ?? DefaultModel,
            [.. messages.Select(m => new OpenAiMessage(m.Role, m.Content))],
            stream,
            0.2,
            1024);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private sealed record OpenAiRequest(string Model, IReadOnlyList<OpenAiMessage> Messages, bool Stream, double Temperature, int MaxTokens);
    private sealed record OpenAiMessage(string Role, string Content);
    private sealed record OpenAiCompletionResponse(IReadOnlyList<OpenAiChoice> Choices);
    private sealed record OpenAiChoice(OpenAiMessageContent Message);
    private sealed record OpenAiMessageContent(string Content);
    private sealed record OpenAiStreamChunk(IReadOnlyList<OpenAiStreamChoice> Choices);
    private sealed record OpenAiStreamChoice(OpenAiStreamDelta Delta);
    private sealed record OpenAiStreamDelta(string? Content);
}
