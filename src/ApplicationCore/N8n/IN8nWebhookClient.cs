namespace CrmAtlas.ApplicationCore.N8n;

public interface IN8nWebhookClient
{
    Task<HttpResponseMessage> TriggerAsync(object payload, CancellationToken cancellationToken = default);
}
