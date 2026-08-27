using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/clientes")]
public sealed class ClientesController(IClienteService service) : ControllerBase
{
    [HttpGet]
    public Task<CursorResult<ClienteDto>> List(
        [FromQuery] ClienteFilter filter,
        CancellationToken cancellationToken) =>
        service.ListAsync(filter, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<ClienteDto> Get(long id, CancellationToken cancellationToken) =>
        service.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(
        ClienteDto dto,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<ClienteDto> Update(long id, ClienteDto dto, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

