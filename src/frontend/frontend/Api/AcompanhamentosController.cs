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
    public Task<IReadOnlyList<AcompanhamentoDto>> List(CancellationToken ct) => service.ListAsync(null, ct);

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
        var items = await service.ListAsync(null, ct);
        var file = reports.GenerateExcel(items);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "acompanhamentos.xlsx");
    }

    [HttpPost("import")]
    public Task<IReadOnlyList<AcompanhamentoDto>> Import(
        IReadOnlyList<AcompanhamentoImportDto> rows, CancellationToken ct) => service.ImportAsync(rows, ct);
}
