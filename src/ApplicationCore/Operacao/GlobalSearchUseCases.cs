using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Servicos;

namespace CrmAtlas.ApplicationCore.Operacao;

public enum GlobalSearchResultType { CLIENTE, SERVICO, ORCAMENTO, PRESTADOR, ACOMPANHAMENTO }
public sealed record GlobalSearchResult(GlobalSearchResultType Tipo,long Id,string Titulo,string? Subtitulo,string? Codigo);

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query,int limitPerType=8,CancellationToken ct=default);
}

public sealed class GlobalSearchService(IClienteService clients,ICadastroServicoService services,
    IOrcamentoService budgets,IPrestadorService providers,IAcompanhamentoService tracking) : IGlobalSearchService
{
    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query,int limitPerType=8,CancellationToken ct=default)
    {
        query=(query??"").Trim();
        if(query.Length<2)return [];
        limitPerType=Math.Clamp(limitPerType,1,20);
        var clientsResult=await clients.ListAsync(new(null,null,null,null,null,null,null,1,100),ct);
        var servicesResult=await services.ListAsync(new(null,null,null,1,100),ct);
        var budgetsResult=await budgets.ListAsync(ct);
        var providersResult=await providers.ListAsync(ct);
        var trackingResult=await tracking.ListAsync(null,ct);
        var result=new List<GlobalSearchResult>();
        result.AddRange(clientsResult.Items.Where(x=>Matches($"{x.RazaoSocial} {x.CnpjCpf} {x.NomeContato} {x.Email} {x.Telefone} {x.Cidade} {x.Estado}",query))
            .Take(limitPerType).Where(x=>x.Id is not null)
            .Select(x=>new GlobalSearchResult(GlobalSearchResultType.CLIENTE,x.Id!.Value,x.RazaoSocial,x.CnpjCpf,x.Cidade)));
        result.AddRange(servicesResult.Items.Where(x=>Matches($"{x.Codigo} {x.RazaoSocialEmpresa} {x.DocumentoEmpresa} {x.Subtipo}",query))
            .Take(limitPerType).Where(x=>x.Id is not null).Select(x=>new GlobalSearchResult(GlobalSearchResultType.SERVICO,x.Id!.Value,x.RazaoSocialEmpresa,x.Subtipo,x.Codigo)));
        result.AddRange(budgetsResult.Where(x=>Matches($"{x.Codigo} {x.Nome} {x.Descricao} {x.Situacao}",query))
            .Take(limitPerType).Where(x=>x.Id is not null).Select(x=>new GlobalSearchResult(GlobalSearchResultType.ORCAMENTO,x.Id!.Value,x.Nome??x.Codigo,x.Situacao,x.Codigo)));
        result.AddRange(providersResult.Where(x=>Matches($"{x.Nome} {x.CnpjCpf} {x.Email} {x.Telefone}",query))
            .Take(limitPerType).Where(x=>x.Id is not null).Select(x=>new GlobalSearchResult(GlobalSearchResultType.PRESTADOR,x.Id!.Value,x.Nome,x.Email,x.CnpjCpf)));
        result.AddRange(trackingResult.Where(x=>Matches($"{x.Codigo} {x.Cliente} {x.Situacao} {x.Descricao}",query))
            .Take(limitPerType).Select(x=>new GlobalSearchResult(GlobalSearchResultType.ACOMPANHAMENTO,x.Id,x.Cliente??x.Codigo,x.Situacao,x.Codigo)));
        return result;
    }
    private static bool Matches(string value,string query)=>value.Contains(query,StringComparison.OrdinalIgnoreCase);
}
