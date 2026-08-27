using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/condicoes-pagamento")]
public sealed class CondicoesPagamentoController(ICondicaoPagamentoService service) : ControllerBase
{
    [HttpGet]
    public Task<CursorResult<CondicaoPagamentoDto>> List(
        [FromQuery] CondicaoPagamentoFilter filter,
        CancellationToken ct) => service.ListAsync(filter, ct);

    [HttpGet("{id:long}")]
    public Task<CondicaoPagamentoDto> Get(long id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<CondicaoPagamentoDto>> Create(CondicaoPagamentoDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<CondicaoPagamentoDto> Update(long id, CondicaoPagamentoDto dto, CancellationToken ct) =>
        service.UpdateAsync(id, dto, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController, Authorize, Route("api/cadastro-servicos")]
public sealed class CadastrosServicoController(ICadastroServicoService service) : ControllerBase
{
    [HttpGet]
    public Task<CursorResult<CadastroServicoDto>> List(
        [FromQuery] CadastroServicoFilter filter,
        CancellationToken ct) => service.ListAsync(filter, ct);

    [HttpGet("subtipos")]
    public Task<IReadOnlyList<CadastroServicoSubtipoConfigDto>> Subtipos(CancellationToken ct) =>
        service.ListSubtiposAsync(ct);

    [HttpGet("{id:long}")]
    public Task<CadastroServicoDto> Get(long id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<CadastroServicoDto>> Create(CadastroServicoDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<CadastroServicoDto> Update(long id, CadastroServicoDto dto, CancellationToken ct) =>
        service.UpdateAsync(id, dto, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

