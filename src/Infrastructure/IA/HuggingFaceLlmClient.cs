using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using CrmAtlas.ApplicationCore.IA;
using Microsoft.Extensions.Options;

namespace CrmAtlas.Infrastructure.IA;

public sealed class HuggingFaceLlmClient(IHttpClientFactory httpClientFactory, IOptions<AtlasAiOptions> options) : ILlmClient
{
    private const string DefaultEndpoint = "https://router.huggingface.co/v1/chat/completions";
    private const string DefaultModel = "google/gemma-2-2b-it";

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
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);

        try
        {
            var request = BuildRequest(messages, stream: false);
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var endpoint = string.IsNullOrWhiteSpace(options.Value.Endpoint) ? DefaultEndpoint : options.Value.Endpoint;
            var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return "Limite de requisições atingido na Hugging Face. " + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return $"Hugging Face retornou erro {response.StatusCode}: {errorBody}. Resposta local:\n\n" + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);
            }

            response.EnsureSuccessStatusCode();

            var completion = await response.Content.ReadFromJsonAsync<OpenAiCompletionResponse>(JsonOptions, cancellationToken);
            return completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
                   ?? "Não foi possível obter resposta do modelo.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return "Limite de requisições atingido na Hugging Face. " + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Não foi possível acessar a Hugging Face ({ex.Message}). Resposta local:\n\n" + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);
        }
    }

    private async Task StreamAsync(ChannelWriter<string> writer, IReadOnlyList<AtlasAiMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = options.Value.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                await StreamFallbackAsync(writer, messages, cancellationToken);
                return;
            }

            var request = BuildRequest(messages, stream: true);
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var endpoint = string.IsNullOrWhiteSpace(options.Value.Endpoint) ? DefaultEndpoint : options.Value.Endpoint;
            var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await writer.WriteAsync("Limite de requisições atingido na Hugging Face. Vou responder com base nos dados do CRM.\n\n", cancellationToken);
                await StreamFallbackAsync(writer, messages, cancellationToken);
                return;
            }

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                await writer.WriteAsync($"Hugging Face retornou erro {response.StatusCode}: {errorBody}. Resposta local:\n\n", cancellationToken);
                await StreamFallbackAsync(writer, messages, cancellationToken);
                return;
            }

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
                    await writer.WriteAsync(delta, cancellationToken);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await writer.WriteAsync("Limite de requisições atingido na Hugging Face. Vou responder com base nos dados do CRM.\n\n", cancellationToken);
            await StreamFallbackAsync(writer, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            await writer.WriteAsync($"Não foi possível acessar a Hugging Face ({ex.Message}). Resposta local:\n\n", cancellationToken);
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

    private OpenAiRequest BuildRequest(IReadOnlyList<AtlasAiMessage> messages, bool stream) =>
        new(
            string.IsNullOrWhiteSpace(options.Value.Model) ? DefaultModel : options.Value.Model,
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
