using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Servicos;

namespace CrmAtlas.ApplicationCore.Financeiro;

public sealed class CustoIndireto : Entity
{
    public DateOnly Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Categoria { get; set; } = string.Empty;
}

public sealed class Lancamento : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public LancamentoTipo Tipo { get; set; }
    public LancamentoStatus Status { get; set; }
    public LancamentoOrigem Origem { get; set; } = LancamentoOrigem.MANUAL;
    public long? CadastroServicoId { get; set; }
    public CadastroServico? CadastroServico { get; set; }
    public long? PrestadorId { get; set; }
    public Prestador? Prestador { get; set; }
    public string? CodigoServico { get; set; }
    public string? NomeCliente { get; set; }
    public string? NomePrestador { get; set; }
    public string? Descricao { get; set; }
    public decimal? Valor { get; set; }
    public DateOnly? Data { get; set; }
    public int? NumeroParcela { get; set; }
    public DateOnly? DataPrevistaParcela { get; set; }
    public string? FormaPagamento { get; set; }
    public string? MetodoPagamento { get; set; }
    public string? Plataforma { get; set; }
    public string? Empresa { get; set; }
    public string? ComprovanteUrl { get; set; }
    public string? ComprovanteNomeArquivo { get; set; }
    public string? Observacao { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
