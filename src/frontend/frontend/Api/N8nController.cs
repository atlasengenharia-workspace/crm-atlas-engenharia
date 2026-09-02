using CrmAtlas.ApplicationCore.IA;
using CrmAtlas.ApplicationCore.N8n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace CrmAtlas.Web.Api;

[ApiController, Route("api/n8n")]
public sealed class N8nController(IAtlasAiService aiService, IContextRetriever contextRetriever, IOptions<N8nOptions> options) : ControllerBase
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

    [AllowAnonymous, HttpPost("ask")]
    public async Task<IActionResult> Ask(
        [FromBody] N8nQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateSecret())
            return Unauthorized();

        var question = request.Question ?? request.Payload?.ToString() ?? string.Empty;
        var answer = await aiService.AskNonStreamingAsync(question, cancellationToken);
        return Ok(new { question, answer });
    }

    [AllowAnonymous, HttpPost("context")]
    public async Task<IActionResult> Context(
        [FromBody] N8nQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateSecret())
            return Unauthorized();

        var question = request.Question ?? request.Payload?.ToString() ?? string.Empty;
        var context = await contextRetriever.RetrieveAsync(question, cancellationToken);
        return Ok(new { question, context });
    }

    [AllowAnonymous, HttpPost("trigger")]
    public IActionResult Trigger([FromBody] object payload)
    {
        if (!ValidateSecret())
            return Unauthorized();

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
