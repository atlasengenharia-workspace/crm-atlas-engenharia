using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CrmAtlas.ApplicationCore.Clientes;

namespace CrmAtlas.Infrastructure.Integrations;

public sealed class ViaCepLookupService(HttpClient httpClient) : ICepLookupService
{
    public async Task<CepAddress?> FindAsync(string cep, CancellationToken cancellationToken = default)
    {
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        if (digits.Length != 8) throw new ArgumentException("Informe um CEP com 8 dígitos.");
        var result = await httpClient.GetFromJsonAsync<ViaCepResponse>($"{digits}/json/", cancellationToken);
        if (result is null || result.Error) return null;
        return new CepAddress(digits, result.Street, result.District, result.City, result.State);
    }

    private sealed record ViaCepResponse(
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("logradouro")] string? Street,
        [property: JsonPropertyName("bairro")] string? District,
        [property: JsonPropertyName("localidade")] string? City,
        [property: JsonPropertyName("uf")] string? State,
        [property: JsonPropertyName("erro")] bool Error);
}
