using System.Net.Http.Json;
using CrmAtlas.ApplicationCore.N8n;
using Microsoft.Extensions.Options;

namespace CrmAtlas.Infrastructure.N8n;

public sealed class N8nWebhookClient(HttpClient client, IOptions<N8nOptions> options) : IN8nWebhookClient
{
    public async Task<HttpResponseMessage> TriggerAsync(object payload, CancellationToken cancellationToken = default)
    {
        var url = options.Value.WebhookUrl;
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("N8N:WebhookUrl não está configurado.");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);

        var apiKey = options.Value.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Add("X-N8N-API-KEY", apiKey);

        return await client.SendAsync(request, cancellationToken);
    }
}
