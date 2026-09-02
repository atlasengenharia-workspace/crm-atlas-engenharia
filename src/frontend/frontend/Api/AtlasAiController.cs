using CrmAtlas.ApplicationCore.IA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/atlas-ai")]
public sealed class AtlasAiController(IAtlasAiService aiService) : ControllerBase
{
    [HttpPost("ask")]
    public async IAsyncEnumerable<string> Ask(
        [FromBody] AtlasAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var chunk in aiService.AskAsync(request.Question, cancellationToken))
        {
            yield return chunk;
        }
    }

    public sealed record AtlasAiRequest(string Question);
}
