using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/custos-indiretos")]
public sealed class CustosIndiretosController(ICustoIndiretoService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CustoIndiretoDto>> List(
        [FromQuery] CustoIndiretoFilter filter,
        CancellationToken ct) => service.ListAsync(filter, ct);

    [HttpGet("{id:long}")]
    public Task<CustoIndiretoDto> Get(long id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<CustoIndiretoDto>> Create(CustoIndiretoDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<CustoIndiretoDto> Update(long id, CustoIndiretoDto dto, CancellationToken ct) =>
        service.UpdateAsync(id, dto, ct);

    [HttpPost("import")]
    public async Task<ActionResult<IReadOnlyList<CustoIndiretoDto>>> Import(
        IReadOnlyList<CustoIndiretoDto> rows,
        CancellationToken ct) => Created(string.Empty, await service.ImportAsync(rows, ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController, Authorize, Route("api/lancamentos")]
public sealed class LancamentosController(ILancamentoService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<LancamentoDto>> List(
        [FromQuery] LancamentoFilter filter,
        CancellationToken ct) => service.ListAsync(filter, ct);

    [HttpGet("{id:long}")]
    public Task<LancamentoDto> Get(long id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<LancamentoDto>> Create(LancamentoDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<LancamentoDto> Update(long id, LancamentoDto dto, CancellationToken ct) =>
        service.UpdateAsync(id, dto, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

