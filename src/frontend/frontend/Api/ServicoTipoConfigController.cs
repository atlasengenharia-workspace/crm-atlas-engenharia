using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController]
[Route("api/servico-tipo-config")]
[Authorize]
public class ServicoTipoConfigController(IServicoTipoConfigService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] IReadOnlyList<ServicoTipoConfigDto> configs, CancellationToken ct)
    {
        await service.SaveAsync(configs, ct);
        return Ok();
    }
}
