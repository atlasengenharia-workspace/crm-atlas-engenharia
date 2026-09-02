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
    private const string DefaultEndpoint = "https://router.huggingface.co/hf-inference/models/";
    private const string DefaultModel = "mistralai/Mistral-7B-Instruct-v0.2";

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
            var request = BuildRequest(messages);
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var model = string.IsNullOrWhiteSpace(options.Value.Model) ? DefaultModel : options.Value.Model;
            var endpoint = string.IsNullOrWhiteSpace(options.Value.Endpoint) ? DefaultEndpoint + model : options.Value.Endpoint;

            var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return "Limite de requisições atingido na Hugging Face. " + await new FallbackLlmClient().CompleteAsync(messages, cancellationToken);

            response.EnsureSuccessStatusCode();

            var completion = await response.Content.ReadFromJsonAsync<IReadOnlyList<HuggingFaceCompletionResponse>>(JsonOptions, cancellationToken);
            return completion?.FirstOrDefault()?.GeneratedText?.Trim()
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

            var request = BuildRequest(messages);
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var model = string.IsNullOrWhiteSpace(options.Value.Model) ? DefaultModel : options.Value.Model;
            var endpoint = string.IsNullOrWhiteSpace(options.Value.Endpoint) ? DefaultEndpoint + model : options.Value.Endpoint;

            var response = await client.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await writer.WriteAsync("Limite de requisições atingido na Hugging Face. Vou responder com base nos dados do CRM.\n\n", cancellationToken);
                await StreamFallbackAsync(writer, messages, cancellationToken);
                return;
            }

            response.EnsureSuccessStatusCode();

            var completion = await response.Content.ReadFromJsonAsync<IReadOnlyList<HuggingFaceCompletionResponse>>(JsonOptions, cancellationToken);
            var text = completion?.FirstOrDefault()?.GeneratedText?.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    await writer.WriteAsync(word + " ", cancellationToken);
                }
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

    private HuggingFaceRequest BuildRequest(IReadOnlyList<AtlasAiMessage> messages)
    {
        var prompt = BuildMistralPrompt(messages);
        return new HuggingFaceRequest(
            prompt,
            new HuggingFaceParameters(0.2, 1024, false));
    }

    private static string BuildMistralPrompt(IReadOnlyList<AtlasAiMessage> messages)
    {
        var sb = new StringBuilder();
        sb.Append("<s>");

        var system = messages.FirstOrDefault(m => m.Role == "system")?.Content;
        if (!string.IsNullOrWhiteSpace(system))
        {
            sb.Append("[INST] ");
            sb.Append(EscapeMistral(system));
            sb.Append(" [/INST]");
            sb.Append("</s>");
        }

        foreach (var message in messages.Where(m => m.Role is "user" or "assistant"))
        {
            if (message.Role == "user")
            {
                sb.Append("<s>[INST] ");
                sb.Append(EscapeMistral(message.Content));
                sb.Append(" [/INST]");
            }
            else
            {
                sb.Append(" ");
                sb.Append(EscapeMistral(message.Content));
                sb.Append("</s>");
            }
        }

        return sb.ToString();
    }

    private static string EscapeMistral(string text) =>
        text.Replace("[", " ").Replace("]", " ").Replace("</s>", " ").Replace("<s>", " ");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private sealed record HuggingFaceRequest(string Inputs, HuggingFaceParameters Parameters);
    private sealed record HuggingFaceParameters(double Temperature, int MaxNewTokens, [property: JsonPropertyName("return_full_text")] bool ReturnFullText);
    private sealed record HuggingFaceCompletionResponse([property: JsonPropertyName("generated_text")] string? GeneratedText);
}
