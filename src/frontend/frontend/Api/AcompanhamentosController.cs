using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Operacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/acompanhamentos")]
public sealed class AcompanhamentosController(
    IAcompanhamentoService service,
    IAcompanhamentoReportService reports) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AcompanhamentoDto>> List(
        [FromQuery] AcompanhamentoFilter filter,
        CancellationToken ct) => service.ListAsync(filter, ct);

    [HttpGet("{id:long}")]
    public Task<AcompanhamentoDto> Get(long id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet("{id:long}/relatorio")]
    public async Task<IActionResult> Report(long id, CancellationToken ct)
    {
        var item = await service.GetAsync(id, ct);
        return File(reports.GeneratePdf(item), "application/pdf", $"acompanhamento-{item.Codigo}.pdf");
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(CancellationToken ct)
    {
        var result = await service.ListAsync(new AcompanhamentoFilter { PageSize = 5000 }, ct);
        var file = reports.GenerateExcel(result.Items);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "acompanhamentos.xlsx");
    }

    [HttpPost("import")]
    public Task<IReadOnlyList<AcompanhamentoDto>> Import(
        IReadOnlyList<AcompanhamentoImportDto> rows, CancellationToken ct) => service.ImportAsync(rows, ct);
}
