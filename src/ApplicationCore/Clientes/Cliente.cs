using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Clientes;

public sealed class Cliente : Entity
{
    public string CnpjCpf { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeContato { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Complemento { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
}
