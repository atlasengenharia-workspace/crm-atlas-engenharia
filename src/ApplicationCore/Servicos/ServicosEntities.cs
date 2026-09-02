using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Servicos;

public sealed class Avcb : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string? NomeCliente { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Servico { get; set; }
    public SituacaoAvcb? Situacao { get; set; }
    public string? DescricaoSituacao { get; set; }
    public decimal? ValorContrato { get; set; }
    public DateOnly? DataContrato { get; set; }
    public string? Nf { get; set; }
    public string? CondicaoPagamento { get; set; }
    public decimal? AReceber { get; set; }
    public decimal? Recebido { get; set; }
    public decimal? Custos { get; set; }
}

public sealed class Clcb : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string? NomeCliente { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public SituacaoClcb? Situacao { get; set; }
    public string? DescricaoSituacao { get; set; }
    public decimal? ValorContrato { get; set; }
    public string? Nf { get; set; }
    public DateOnly? DataContrato { get; set; }
    public decimal? AReceber { get; set; }
    public decimal? Recebido { get; set; }
    public decimal? Custos { get; set; }
}

public sealed class Obra : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string? NomeCliente { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Servico { get; set; }
    public SituacaoObra? Situacao { get; set; }
    public string? DescricaoSituacao { get; set; }
    public decimal? ValorContrato { get; set; }
    public DateOnly? DataContrato { get; set; }
    public string? Nf { get; set; }
    public string? CondicaoPagamento { get; set; }
    public decimal? AReceber { get; set; }
    public decimal? Recebido { get; set; }
    public decimal? Custos { get; set; }
}

public sealed class ProcessoAdm : Entity
{
    public SituacaoProcesso? Situacao { get; set; }
    public string? DescricaoSituacao { get; set; }
    public string? NomeCliente { get; set; }
    public string? Codigo { get; set; }
    public string? Servico { get; set; }
    public decimal? ValorContrato { get; set; }
    public DateOnly? DataContrato { get; set; }
    public string? Nf { get; set; }
    public string? CondicaoPagamento { get; set; }
    public DateOnly? ProximaParcela { get; set; }
    public decimal? AReceber { get; set; }
    public decimal? Recebido { get; set; }
    public decimal? Custos { get; set; }
}

public sealed class CondicaoPagamento : Entity
{
    public string Nome { get; set; } = string.Empty;
    public int? QuantidadeParcelas { get; set; }
    public int? IntervaloDias { get; set; }
    public bool Indefinido { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Orcamento : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateOnly? Data { get; set; }
    public AcompanhamentoServicoTipo TipoServico { get; set; }
    public decimal? ValorTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class OrcamentoSituacao : Entity
{
    public string Label { get; set; } = string.Empty;
    public bool Closed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CadastroServico : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public long? OrcamentoId { get; set; }
    public Orcamento? Orcamento { get; set; }
    public long? CondicaoPagamentoId { get; set; }
    public CondicaoPagamento? CondicaoPagamento { get; set; }
    public AcompanhamentoServicoTipo TipoServico { get; set; }
    public string? Subtipo { get; set; }
    public DateOnly? DataEntrada { get; set; }
    public string? SituacaoInicial { get; set; }
    public string? DocumentoEmpresa { get; set; }
    public string? RazaoSocialEmpresa { get; set; }
    public string? ContatoEmpresa { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? EnderecoEmpresa { get; set; }
    public string? EnderecoEmpresaRua { get; set; }
    public string? EnderecoEmpresaNumero { get; set; }
    public string? EnderecoEmpresaBairro { get; set; }
    public string? EnderecoEmpresaComplemento { get; set; }
    public string? EnderecoEmpresaCidade { get; set; }
    public string? EnderecoEmpresaEstado { get; set; }
    public string? EnderecoEmpresaCep { get; set; }
    public string? EnderecoServico { get; set; }
    public string? EnderecoServicoRua { get; set; }
    public string? EnderecoServicoNumero { get; set; }
    public string? EnderecoServicoBairro { get; set; }
    public string? EnderecoServicoComplemento { get; set; }
    public string? EnderecoServicoCidade { get; set; }
    public string? EnderecoServicoEstado { get; set; }
    public string? EnderecoServicoCep { get; set; }
    public bool MesmoEnderecoEmpresa { get; set; }
    public decimal? ValorContrato { get; set; }
    public DateOnly? DataContrato { get; set; }
    public string? NomeCondicaoPagamento { get; set; }
    public decimal? ValorNotaFiscal { get; set; }
    public string? Observacao { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<CadastroServicoParcela> Parcelas { get; set; } = [];
    public ICollection<CadastroServicoPrestador> Prestadores { get; set; } = [];
    public ICollection<CadastroServicoCodigoHistorico> CodigoHistorico { get; set; } = [];
}

public sealed class CadastroServicoCodigoHistorico : Entity
{
    public long ServicoId { get; set; }
    public CadastroServico Servico { get; set; } = null!;
    public string? CodigoAnterior { get; set; }
    public string? CodigoNovo { get; set; }
    public string? Responsavel { get; set; }
    public DateTime AlteradoEm { get; set; }
}

public sealed class CadastroServicoParcela : Entity
{
    public long CadastroServicoId { get; set; }
    public CadastroServico CadastroServico { get; set; } = null!;
    public int? NumeroParcela { get; set; }
    public decimal? Valor { get; set; }
    public DateOnly? DataVencimento { get; set; }
    public string? FormaPagamento { get; set; }
}

public sealed class Prestador : Entity
{
    public string? Nome { get; set; }
    public string? CnpjCpf { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? MetodoPagamento { get; set; }
    public string? ChavePix { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CadastroServicoPrestador : Entity
{
    public long CadastroServicoId { get; set; }
    public CadastroServico CadastroServico { get; set; } = null!;
    public long? PrestadorId { get; set; }
    public Prestador? Prestador { get; set; }
    public string? NomePrestador { get; set; }
    public decimal? ValorProvisionado { get; set; }
    public decimal? ValorEfetivo { get; set; }
    public bool? Confirmado { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public PrestadorPagamentoDataTipo DataPagamentoTipo { get; set; } = PrestadorPagamentoDataTipo.A_DEFINIR;
}
