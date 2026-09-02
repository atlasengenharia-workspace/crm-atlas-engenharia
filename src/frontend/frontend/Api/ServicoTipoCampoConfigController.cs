using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController]
[Route("api/servico-tipo-campo-config")]
[Authorize]
public class ServicoTipoCampoConfigController(IServicoTipoCampoConfigService service) : ControllerBase
{
    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaults(CancellationToken ct)
    {
        var result = await service.GetDefaultsAsync(ct);
        return Ok(result);
    }

    [HttpGet("tipo/{tipo}")]
    public async Task<IActionResult> GetByTipo(AcompanhamentoServicoTipo tipo, CancellationToken ct)
    {
        var result = await service.ListByTipoAsync(tipo, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] IReadOnlyList<ServicoTipoCampoConfigDto> configs, CancellationToken ct)
    {
        await service.SaveAsync(configs, ct);
        return Ok();
    }
}
