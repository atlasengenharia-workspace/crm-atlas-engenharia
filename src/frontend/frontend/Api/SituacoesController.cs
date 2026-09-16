using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.ApplicationCore.Sistema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/orcamento-situacoes")]
public sealed class OrcamentoSituacoesController(IOrcamentoSituacaoService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<OrcamentoSituacaoDto>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpPost]
    public Task<OrcamentoSituacaoDto> Save(OrcamentoSituacaoDto dto, CancellationToken ct) => service.SaveAsync(dto, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController, Authorize, Route("api/configuracao-historico")]
public sealed class ConfiguracaoHistoricoController(IConfiguracaoHistoricoService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ConfiguracaoHistoricoDto>> List(
        [FromQuery] string? contexto,
        [FromQuery] int take,
        CancellationToken ct) => service.ListAsync(contexto, take is 0 ? 200 : take, ct);
}

[ApiController, Authorize, Route("api/registro-historico")]
public sealed class RegistroHistoricoController(IRegistroHistoricoService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RegistroHistoricoDto>> List(
        [FromQuery] string? entidade,
        [FromQuery] long? entidadeId,
        [FromQuery] int take,
        CancellationToken ct) => service.ListAsync(entidade, entidadeId, take is 0 ? 500 : take, ct);
}
