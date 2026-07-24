using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Acompanhamentos;

public sealed class AcompanhamentoServico : Entity
{
    public AcompanhamentoServicoTipo TipoServico { get; set; }
    public long OrigemId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? NomeCliente { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Subtipo { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorContrato { get; set; }
    public DateOnly? DataContrato { get; set; }
    public string? NotaFiscal { get; set; }
    public string? CondicaoPagamento { get; set; }
    public decimal? AReceber { get; set; }
    public decimal? Recebido { get; set; }
    public decimal? Custos { get; set; }
    public string? FolderUrl { get; set; }
    public DateTime? UltimaMudancaSituacaoEm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<AcompanhamentoServicoHistorico> Historicos { get; set; } = [];
    public ICollection<AcompanhamentoServicoPendencia> Pendencias { get; set; } = [];
}

public sealed class AcompanhamentoServicoHistorico : Entity
{
    public long ServicoId { get; set; }
    public AcompanhamentoServico Servico { get; set; } = null!;
    public string? SituacaoAnterior { get; set; }
    public string NovaSituacao { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AcompanhamentoServicoSituacaoConfig : Entity
{
    public AcompanhamentoServicoTipo TipoServico { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? Ordem { get; set; }
    public bool SituacaoInicial { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<AcompanhamentoSituacaoPendenciaConfig> Pendencias { get; set; } = [];
}

public sealed class AcompanhamentoSituacaoPendenciaConfig : Entity
{
    public long SituacaoConfigId { get; set; }
    public AcompanhamentoServicoSituacaoConfig SituacaoConfig { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public int? Ordem { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AcompanhamentoServicoPendencia : Entity
{
    public long ServicoId { get; set; }
    public AcompanhamentoServico Servico { get; set; } = null!;
    public long? SituacaoConfigId { get; set; }
    public AcompanhamentoServicoSituacaoConfig? SituacaoConfig { get; set; }
    public long? PendenciaConfigId { get; set; }
    public AcompanhamentoSituacaoPendenciaConfig? PendenciaConfig { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool Concluida { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
