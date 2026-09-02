using CrmAtlas.ApplicationCore.IA;
using CrmAtlas.ApplicationCore.N8n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace CrmAtlas.Web.Api;

[ApiController, Route("api/n8n")]
public sealed class N8nController(IAtlasAiService aiService, IOptions<N8nOptions> options) : ControllerBase
{
    [AllowAnonymous, HttpPost("query")]
    public async IAsyncEnumerable<string> Query(
        [FromBody] N8nQueryRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!ValidateSecret())
        {
            yield return "Unauthorized";
            yield break;
        }

        var question = request.Question ?? request.Payload?.ToString() ?? string.Empty;
        await foreach (var chunk in aiService.AskAsync(question, cancellationToken))
        {
            yield return chunk;
        }
    }

    [AllowAnonymous, HttpPost("trigger")]
    public IActionResult Trigger([FromBody] object payload)
    {
        if (!ValidateSecret())
            return Unauthorized();

        // O payload pode ser processado aqui (ex: salvar log, executar uma ação).
        // Por padrão, apenas aceitamos o trigger e retornamos 200.
        return Ok(new { received = true });
    }

    private bool ValidateSecret()
    {
        var configured = options.Value.IncomingSecret;
        if (string.IsNullOrWhiteSpace(configured)) return true;

        var header = Request.Headers["X-N8N-SECRET"].FirstOrDefault();
        var query = Request.Query["secret"].FirstOrDefault();
        return header == configured || query == configured;
    }

    public sealed record N8nQueryRequest(string? Question, object? Payload);
}
