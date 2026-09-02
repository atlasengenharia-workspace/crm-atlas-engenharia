using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrmAtlas.ApplicationCore.Integracoes;

namespace CrmAtlas.Infrastructure.Integracoes;

public sealed class GoogleAdsApiClient(IHttpClientFactory httpClientFactory) : IGoogleAdsApiClient
{
    private const string ApiBaseUrl = "https://googleads.googleapis.com/v25";
    private const string OAuthTokenUrl = "https://oauth2.googleapis.com/token";
    private const string OAuthAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";

    public Task TestConnectionAsync(GoogleAdsIntegrationConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.DeveloperToken))
            throw new InvalidOperationException("Developer Token é obrigatório.");
        if (string.IsNullOrWhiteSpace(config.RefreshToken))
            throw new InvalidOperationException("A integração não possui refresh token.");
        if (string.IsNullOrWhiteSpace(config.LoginCustomerId))
            throw new InvalidOperationException("Login Customer ID é obrigatório.");

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<GoogleAdsCampaignData>> GetCampaignsAsync(GoogleAdsIntegrationConfig config, CancellationToken cancellationToken = default)
    {
        var customerId = config.LoginCustomerId!;
        const string query = """
            SELECT
                campaign.id,
                campaign.name,
                campaign.status,
                campaign.advertising_channel_type,
                campaign.start_date,
                campaign.end_date,
                campaign_budget.amount_micros
            FROM campaign
            ORDER BY campaign.id
            """;

        var rows = await SearchStreamAsync(config, customerId, query, cancellationToken);
        var results = new List<GoogleAdsCampaignData>();

        foreach (var row in rows)
        {
            var campaign = row.GetPropertyOrNull("campaign")?.ValueKind == JsonValueKind.Object ? row.GetProperty("campaign") : default;
            var budget = row.GetPropertyOrNull("campaignBudget")?.ValueKind == JsonValueKind.Object ? row.GetProperty("campaignBudget") : default;

            if (campaign.ValueKind == JsonValueKind.Undefined) continue;

            results.Add(new GoogleAdsCampaignData(
                campaign.GetInt64OrDefault("id"),
                campaign.GetStringOrDefault("name") ?? string.Empty,
                campaign.GetStringOrDefault("status") ?? string.Empty,
                campaign.GetStringOrDefault("advertisingChannelType"),
                ParseDate(campaign.GetStringOrDefault("startDate")),
                ParseDate(campaign.GetStringOrDefault("endDate")),
                budget.ValueKind == JsonValueKind.Undefined ? null : budget.GetInt64OrDefault("amountMicros")));
        }

        return results;
    }

    public async Task<IReadOnlyList<GoogleAdsMetricData>> GetMetricsAsync(GoogleAdsIntegrationConfig config, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        var customerId = config.LoginCustomerId!;
        var query = $"""
            SELECT
                campaign.id,
                campaign.name,
                segments.date,
                metrics.cost_micros,
                metrics.clicks,
                metrics.impressions,
                metrics.conversions,
                metrics.conversions_value,
                metrics.all_conversions,
                metrics.ctr,
                metrics.average_cpm,
                metrics.average_cpc
            FROM campaign
            WHERE segments.date BETWEEN '{start:yyyy-MM-dd}' AND '{end:yyyy-MM-dd}'
            ORDER BY campaign.id, segments.date
            """;

        var rows = await SearchStreamAsync(config, customerId, query, cancellationToken);
        var results = new List<GoogleAdsMetricData>();

        foreach (var row in rows)
        {
            var campaign = row.GetPropertyOrNull("campaign")?.ValueKind == JsonValueKind.Object ? row.GetProperty("campaign") : default;
            var metrics = row.GetPropertyOrNull("metrics")?.ValueKind == JsonValueKind.Object ? row.GetProperty("metrics") : default;
            var segments = row.GetPropertyOrNull("segments")?.ValueKind == JsonValueKind.Object ? row.GetProperty("segments") : default;

            if (campaign.ValueKind == JsonValueKind.Undefined || metrics.ValueKind == JsonValueKind.Undefined || segments.ValueKind == JsonValueKind.Undefined)
                continue;

            results.Add(new GoogleAdsMetricData(
                campaign.GetInt64OrDefault("id"),
                campaign.GetStringOrDefault("name") ?? string.Empty,
                ParseDateRequired(segments.GetStringOrDefault("date")),
                metrics.GetInt64OrDefault("costMicros"),
                metrics.GetInt64OrDefault("clicks"),
                metrics.GetInt64OrDefault("impressions"),
                metrics.GetDoubleOrDefault("conversions"),
                metrics.GetDoubleOrDefault("conversionsValue"),
                metrics.GetDoubleOrDefault("allConversions"),
                metrics.GetDoubleOrDefault("ctr"),
                metrics.GetDoubleOrDefault("averageCpm"),
                metrics.GetDoubleOrDefault("averageCpc")));
        }

        return results;
    }

    public Task<IReadOnlyList<GoogleAdsLeadData>> GetLeadsAsync(GoogleAdsIntegrationConfig config, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        // Leads com dados pessoais exigem integracao via Webhook ou CRM via Google Ads Lead Form.
        // A API REST do Google Ads nao expoe dados de leads preenchidos.
        // Retornamos vazio ate configurar webhook.
        return Task.FromResult<IReadOnlyList<GoogleAdsLeadData>>([]);
    }

    public async Task<string> BuildAuthorizationUrlAsync(GoogleAdsIntegrationConfig config, string redirectUri, string state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.ClientId))
            throw new InvalidOperationException("Client ID é obrigatório.");

        var scopes = "https://www.googleapis.com/auth/adwords";
        var url = $"{OAuthAuthUrl}?client_id={Uri.EscapeDataString(config.ClientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}&access_type=offline&prompt=consent&state={Uri.EscapeDataString(state)}";
        return await Task.FromResult(url);
    }

    public async Task<string> ExchangeCodeAsync(GoogleAdsIntegrationConfig config, string redirectUri, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
            throw new InvalidOperationException("Client ID e Client Secret são obrigatórios.");

        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync(OAuthTokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        }), cancellationToken);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia ao trocar código.");

        if (string.IsNullOrWhiteSpace(token.RefreshToken))
            throw new InvalidOperationException("O Google não retornou refresh token.");

        return token.RefreshToken;
    }

    public async Task<string> GetAccessTokenAsync(GoogleAdsIntegrationConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
            throw new InvalidOperationException("Client ID e Client Secret são obrigatórios.");
        if (string.IsNullOrWhiteSpace(config.RefreshToken))
            throw new InvalidOperationException("Refresh token ausente.");

        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync(OAuthTokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["refresh_token"] = config.RefreshToken,
            ["grant_type"] = "refresh_token"
        }), cancellationToken);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia ao renovar token.");

        return token.AccessToken ?? throw new InvalidOperationException("Access token ausente.");
    }

    private async Task<IReadOnlyList<JsonElement>> SearchStreamAsync(GoogleAdsIntegrationConfig config, string customerId, string query, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(config, cancellationToken);
        var url = $"{ApiBaseUrl}/customers/{customerId}/googleAds:searchStream";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("developer-token", config.DeveloperToken);
        client.DefaultRequestHeaders.Add("login-customer-id", customerId);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync(url, new { query }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Ads API error: {response.StatusCode} - {body}");

        var results = new List<JsonElement>();
        var batchResponses = JsonDocument.Parse(body).RootElement.EnumerateArray();

        foreach (var batch in batchResponses)
        {
            if (batch.TryGetProperty("results", out var rows))
            {
                foreach (var row in rows.EnumerateArray())
                    results.Add(row.Clone());
            }
        }

        return results;
    }

    private static DateOnly? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly ParseDateRequired(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Data do segmento vazia.")
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}

file static class JsonExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        return element.TryGetProperty(propertyName, out var value) ? value : null;
    }

    public static string? GetStringOrDefault(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return null;
        if (!element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    public static long GetInt64OrDefault(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return 0;
        if (!element.TryGetProperty(propertyName, out var value)) return 0;
        return value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var parsed)
            ? parsed
            : value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;
    }

    public static double GetDoubleOrDefault(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return 0;
        if (!element.TryGetProperty(propertyName, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;
    }
}
