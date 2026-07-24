using CrmAtlas.ApplicationCore.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/comprovantes")]
public sealed class ReceiptsController(IReceiptStorage storage) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Download(string key, CancellationToken cancellationToken)
    {
        var file = await storage.OpenReadAsync(key, cancellationToken);
        return file is null
            ? NotFound()
            : File(file.Value.Content, file.Value.ContentType, enableRangeProcessing: true);
    }
}
