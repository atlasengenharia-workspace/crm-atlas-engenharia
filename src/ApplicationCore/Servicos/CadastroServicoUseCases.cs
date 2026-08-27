using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Servicos;

public sealed record CadastroServicoParcelaDto(
    long? Id,
    int? NumeroParcela,
    decimal? Valor,
    DateOnly? DataVencimento,
    string? FormaPagamento);

public sealed record CadastroServicoPrestadorDto(
    long? Id,
    long? PrestadorId,
    string? NomePrestador,
    decimal? ValorProvisionado,
    decimal? ValorEfetivo,
    bool? Confirmado,
    DateOnly? DataPagamento,
    PrestadorPagamentoDataTipo DataPagamentoTipo = PrestadorPagamentoDataTipo.A_DEFINIR);

public sealed record CadastroServicoDto(
    long? Id,
    string? Codigo,
    long? ClienteId,
    long? OrcamentoId,
    string? OrcamentoCodigo,
    long? CondicaoPagamentoId,
    AcompanhamentoServicoTipo TipoServico,
    [Required] string Subtipo,
    DateOnly DataEntrada,
    string? SituacaoInicial,
    string? DocumentoEmpresa,
    [Required] string RazaoSocialEmpresa,
    string? ContatoEmpresa,
    string? Telefone,
    [EmailAddress] string? Email,
    string? EnderecoEmpresa,
    string? EnderecoEmpresaRua,
    string? EnderecoEmpresaNumero,
    string? EnderecoEmpresaBairro,
    string? EnderecoEmpresaComplemento,
    string? EnderecoEmpresaCidade,
    [StringLength(2)] string? EnderecoEmpresaEstado,
    string? EnderecoEmpresaCep,
    string? EnderecoServico,
    bool MesmoEnderecoEmpresa,
    decimal? ValorContrato,
    DateOnly? DataContrato,
    string? NomeCondicaoPagamento,
    decimal? ValorNotaFiscal,
    IReadOnlyList<CadastroServicoParcelaDto> Parcelas,
    IReadOnlyList<CadastroServicoPrestadorDto> Prestadores,
    DateTime? CreatedAt);

public sealed record CadastroServicoFilter(
    string? Codigo,
    string? DocumentoEmpresa,
    AcompanhamentoServicoTipo? TipoServico,
    int Page = 1,
    int PageSize = 20,
    string? Situacao = null,
    bool OcultarConcluidos = true);

public sealed record CadastroServicoSubtipoConfigDto(
    AcompanhamentoServicoTipo TipoServico,
    IReadOnlyList<string> Subtipos);

public interface ICadastroServicoRepository : IRepository<CadastroServico>
{
    Task<IReadOnlyList<CadastroServico>> ListDetailedAsync(CancellationToken cancellationToken = default);
    Task<CadastroServico?> GetDetailedAsync(long id, CancellationToken cancellationToken = default);
}

public interface ICadastroServicoService
{
    Task<PagedResult<CadastroServicoDto>> ListAsync(CadastroServicoFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CadastroServicoSubtipoConfigDto>> ListSubtiposAsync(CancellationToken cancellationToken = default);
    Task<CadastroServicoDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<CadastroServicoDto> CreateAsync(CadastroServicoDto dto, CancellationToken cancellationToken = default);
    Task<CadastroServicoDto> UpdateAsync(long id, CadastroServicoDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class CadastroServicoService(
    ICadastroServicoRepository repository,
    IRepository<Cliente> clientes,
    IRepository<Orcamento> orcamentos,
    IRepository<CondicaoPagamento> condicoes,
    IRepository<Prestador> prestadores) : ICadastroServicoService
{
    public async Task<PagedResult<CadastroServicoDto>> ListAsync(
        CadastroServicoFilter filter,
        CancellationToken cancellationToken = default)
    {
        var source = (await repository.ListDetailedAsync(cancellationToken))
            .Where(x => MatchesSearch(x, filter.Codigo, filter.DocumentoEmpresa))
            .Where(x => filter.TipoServico is null || x.TipoServico == filter.TipoServico)
            .Where(x => Contains(x.SituacaoInicial, filter.Situacao))
            .Where(x => !filter.OcultarConcluidos || !IsCompleted(x.SituacaoInicial))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ToDto);
        return PagedResult<CadastroServicoDto>.Create(source, filter.Page, filter.PageSize);
    }

    public async Task<IReadOnlyList<CadastroServicoSubtipoConfigDto>> ListSubtiposAsync(
        CancellationToken cancellationToken = default)
    {
        var saved = await repository.ListAsync(cancellationToken);
        return Enum.GetValues<AcompanhamentoServicoTipo>()
            .Select(tipo => new CadastroServicoSubtipoConfigDto(
                tipo,
                Defaults(tipo)
                    .Concat(saved.Where(x => x.TipoServico == tipo).Select(x => x.Subtipo))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()!))
            .ToList();
    }

    public async Task<CadastroServicoDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<CadastroServicoDto> CreateAsync(
        CadastroServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var existing = await repository.ListAsync(cancellationToken);
        var code = Clean(dto.Codigo);
        if (code is null)
        {
            var count = existing.Count(x => x.TipoServico == dto.TipoServico);
            code = $"S-{dto.TipoServico}-{count + 1:0000}";
        }
        if (existing.Any(x => x.Codigo.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Já existe um serviço com o código {code}.");
        var entity = new CadastroServico { Codigo = code };
        await ApplyAsync(entity, dto, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CadastroServicoDto> UpdateAsync(
        long id,
        CadastroServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var entity = await FindAsync(id, cancellationToken);
        await ApplyAsync(entity, dto, cancellationToken);
        var existing = await repository.ListAsync(cancellationToken);
        if (existing.Any(x => x.Codigo.Equals(entity.Codigo, StringComparison.OrdinalIgnoreCase) && x.Id != id))
            throw new ArgumentException($"Já existe um serviço com o código {entity.Codigo}.");
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(
        CadastroServico entity,
        CadastroServicoDto dto,
        CancellationToken cancellationToken)
    {
        entity.Cliente = await ResolveNavigationAsync(entity.Cliente, clientes, dto.ClienteId, "Cliente", cancellationToken);
        entity.ClienteId = dto.ClienteId;
        entity.Orcamento = await ResolveNavigationAsync(entity.Orcamento, orcamentos, dto.OrcamentoId, "Orçamento", cancellationToken);
        entity.OrcamentoId = dto.OrcamentoId;
        entity.Codigo = Clean(dto.Codigo) ?? entity.Codigo;
        entity.CondicaoPagamento = await ResolveNavigationAsync(
            entity.CondicaoPagamento, condicoes, dto.CondicaoPagamentoId, "Condição de pagamento", cancellationToken);
        entity.CondicaoPagamentoId = dto.CondicaoPagamentoId;
        entity.TipoServico = dto.TipoServico;
        entity.Subtipo = Required(dto.Subtipo, "O subtipo é obrigatório.");
        entity.DataEntrada = dto.DataEntrada;
        entity.SituacaoInicial = Clean(dto.SituacaoInicial)
            ?? (dto.TipoServico == AcompanhamentoServicoTipo.OBRAS ? "ORCAMENTO" : "PENDENTE");
        var documento = Clean(dto.DocumentoEmpresa);
        if (!string.IsNullOrWhiteSpace(documento) && !IsCpfOrCnpjValid(documento))
            throw new ArgumentException("O CPF/CNPJ informado é inválido.");
        entity.DocumentoEmpresa = documento;
        entity.RazaoSocialEmpresa = Required(dto.RazaoSocialEmpresa, "A razão social é obrigatória.");
        entity.ContatoEmpresa = Clean(dto.ContatoEmpresa);
        entity.Telefone = Clean(dto.Telefone);
        entity.Email = Clean(dto.Email);
        entity.EnderecoEmpresaRua = Clean(dto.EnderecoEmpresaRua);
        entity.EnderecoEmpresaNumero = Clean(dto.EnderecoEmpresaNumero);
        entity.EnderecoEmpresaBairro = Clean(dto.EnderecoEmpresaBairro);
        entity.EnderecoEmpresaComplemento = Clean(dto.EnderecoEmpresaComplemento);
        entity.EnderecoEmpresaCidade = Clean(dto.EnderecoEmpresaCidade);
        entity.EnderecoEmpresaEstado = Clean(dto.EnderecoEmpresaEstado)?.ToUpperInvariant();
        entity.EnderecoEmpresaCep = Clean(dto.EnderecoEmpresaCep);
        entity.EnderecoEmpresa = FormatAddress(dto);
        entity.EnderecoServico = dto.MesmoEnderecoEmpresa
            ? entity.EnderecoEmpresa
            : Clean(dto.EnderecoServico);
        entity.MesmoEnderecoEmpresa = dto.MesmoEnderecoEmpresa;
        entity.ValorContrato = dto.ValorContrato;
        entity.DataContrato = dto.DataContrato;
        entity.NomeCondicaoPagamento = Clean(dto.NomeCondicaoPagamento);
        entity.ValorNotaFiscal = dto.ValorNotaFiscal;

        entity.Parcelas.Clear();
        foreach (var item in dto.Parcelas)
            entity.Parcelas.Add(new CadastroServicoParcela
            {
                CadastroServico = entity,
                NumeroParcela = item.NumeroParcela,
                Valor = item.Valor,
                DataVencimento = item.DataVencimento,
                FormaPagamento = Clean(item.FormaPagamento)
            });

        entity.Prestadores.Clear();
        foreach (var item in dto.Prestadores)
            entity.Prestadores.Add(new CadastroServicoPrestador
            {
                CadastroServico = entity,
                Prestador = await ResolveAsync(
                    prestadores, item.PrestadorId, "Prestador", cancellationToken),
                PrestadorId = item.PrestadorId,
                NomePrestador = Clean(item.NomePrestador),
                ValorProvisionado = item.ValorProvisionado,
                ValorEfetivo = item.ValorEfetivo,
                Confirmado = item.Confirmado,
                DataPagamento = item.DataPagamento,
                DataPagamentoTipo = item.DataPagamentoTipo
            });

        if (entity.Orcamento is not null)
            entity.Orcamento.Situacao = "Aprovado";
    }

    private static void Validate(CadastroServicoDto dto)
    {
        if (dto.ValorContrato is null) throw new ArgumentException("O valor do contrato é obrigatório.");
        if (dto.DataContrato is null) throw new ArgumentException("A data do contrato é obrigatória.");
        if (dto.Parcelas.Count == 0) throw new ArgumentException("É necessário informar ao menos uma parcela.");
        var total = dto.Parcelas.Sum(x => x.Valor ?? 0);
        var liquido = Math.Max(0, dto.ValorContrato.Value - (dto.ValorNotaFiscal ?? 0));
        if (total != liquido)
            throw new ArgumentException(
                "A soma das parcelas deve fechar exatamente com o valor líquido (valor do contrato - desconto NF).");

        var duplicatedProviders = dto.Prestadores
            .Where(x => x.PrestadorId is not null)
            .GroupBy(x => x.PrestadorId)
            .Any(x => x.Count() > 1);
        if (duplicatedProviders)
            throw new ArgumentException("O mesmo prestador não pode ser vinculado mais de uma vez ao serviço.");

        foreach (var provider in dto.Prestadores)
        {
            if (provider.PrestadorId is null && string.IsNullOrWhiteSpace(provider.NomePrestador))
                throw new ArgumentException("Selecione ou informe o prestador vinculado.");
            if (provider.ValorProvisionado is < 0 || provider.ValorEfetivo is < 0)
                throw new ArgumentException("Os valores do prestador não podem ser negativos.");
            if (provider.DataPagamentoTipo == PrestadorPagamentoDataTipo.DATA && provider.DataPagamento is null)
                throw new ArgumentException("Informe a data específica de pagamento do prestador.");
            if (provider.DataPagamentoTipo != PrestadorPagamentoDataTipo.DATA && provider.DataPagamento is not null)
                throw new ArgumentException("A data só deve ser informada quando o pagamento for em data específica.");
        }
    }

    private async Task<CadastroServico> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetDetailedAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Cadastro de serviço não encontrado com id: {id}.");

    private static async Task<T?> ResolveAsync<T>(
        IRepository<T> source,
        long? id,
        string resource,
        CancellationToken cancellationToken) where T : Entity
    {
        if (id is null) return null;
        return await source.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException($"{resource} não encontrado com id: {id}.");
    }

    private static async Task<T?> ResolveNavigationAsync<T>(
        T? current,
        IRepository<T> source,
        long? id,
        string resource,
        CancellationToken cancellationToken) where T : Entity
    {
        if (id is null) return null;
        if (current?.Id == id.Value) return current;
        return await ResolveAsync(source, id, resource, cancellationToken);
    }

    private static CadastroServicoDto ToDto(CadastroServico x) => new(
        x.Id, x.Codigo, x.ClienteId, x.OrcamentoId, x.Orcamento?.Codigo, x.CondicaoPagamentoId,
        x.TipoServico, x.Subtipo ?? string.Empty, x.DataEntrada ?? default, x.SituacaoInicial,
        x.DocumentoEmpresa ?? string.Empty, x.RazaoSocialEmpresa ?? string.Empty, x.ContatoEmpresa,
        x.Telefone, x.Email, x.EnderecoEmpresa, x.EnderecoEmpresaRua, x.EnderecoEmpresaNumero,
        x.EnderecoEmpresaBairro, x.EnderecoEmpresaComplemento, x.EnderecoEmpresaCidade,
        x.EnderecoEmpresaEstado, x.EnderecoEmpresaCep, x.EnderecoServico, x.MesmoEnderecoEmpresa,
        x.ValorContrato, x.DataContrato, x.NomeCondicaoPagamento, x.ValorNotaFiscal,
        x.Parcelas.Select(p => new CadastroServicoParcelaDto(
            p.Id, p.NumeroParcela, p.Valor, p.DataVencimento, p.FormaPagamento)).ToList(),
        x.Prestadores.Select(p => new CadastroServicoPrestadorDto(
            p.Id, p.PrestadorId, p.NomePrestador, p.ValorProvisionado, p.ValorEfetivo,
            p.Confirmado, p.DataPagamento, p.DataPagamentoTipo)).ToList(),
        x.CreatedAt);

    private static IReadOnlyList<string> Defaults(AcompanhamentoServicoTipo tipo) => tipo switch
    {
        AcompanhamentoServicoTipo.AVCB => ["Projeto", "Renovação", "Regularização"],
        AcompanhamentoServicoTipo.CLCB => ["Projeto", "Renovação", "Ajuste"],
        AcompanhamentoServicoTipo.OBRAS => ["Residencial", "Comercial", "Industrial"],
        _ => ["Contrato", "Renovação", "Regularização"]
    };

    private static bool Contains(string? source, string? value) =>
        string.IsNullOrWhiteSpace(value) || (source?.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool MatchesSearch(CadastroServico item, string? codigo, string? documento)
    {
        var search = !string.IsNullOrWhiteSpace(codigo) ? codigo : documento;
        return string.IsNullOrWhiteSpace(search)
            || Contains(item.Codigo, search)
            || Contains(item.DocumentoEmpresa, search)
            || Contains(item.RazaoSocialEmpresa, search)
            || Contains(item.Subtipo, search);
    }

    private static bool IsCompleted(string? status)
    {
        var normalized = status?.Trim().TrimEnd('.', ';', ':');
        return string.Equals(normalized, "Concluído", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Concluido", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Finalizado", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Encerrado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpfOrCnpjValid(string? value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length != 11 && digits.Length != 14) return false;
        if (new HashSet<char>(digits).Count == 1) return false;
        if (digits.Length == 11)
        {
            int[] w1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] w2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
            var baseCpf = digits[..9];
            int calc(string input, int[] weights)
            {
                int s = 0;
                for (int i = 0; i < weights.Length; i++) s += (input[i] - '0') * weights[i];
                int r = s % 11;
                return r < 2 ? 0 : 11 - r;
            }
            int d1 = calc(baseCpf, w1);
            int d2 = calc(baseCpf + d1, w2);
            return digits[9] - '0' == d1 && digits[10] - '0' == d2;
        }
        else
        {
            int[] w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] w2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            var baseCnpj = digits[..12];
            int calc(string input, int[] weights)
            {
                int s = 0;
                for (int i = 0; i < weights.Length; i++) s += (input[i] - '0') * weights[i];
                int r = s % 11;
                return r < 2 ? 0 : 11 - r;
            }
            int d1 = calc(baseCnpj, w1);
            int d2 = calc(baseCnpj + d1, w2);
            return digits[12] - '0' == d1 && digits[13] - '0' == d2;
        }
    }

    private static string Required(string? value, string message) =>
        Clean(value) ?? throw new ArgumentException(message);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatAddress(CadastroServicoDto dto)
    {
        var street = string.Join(", ", new[] { Clean(dto.EnderecoEmpresaRua), Clean(dto.EnderecoEmpresaNumero) }
            .Where(x => x is not null));
        var locality = string.Join(" - ", new[] { Clean(dto.EnderecoEmpresaBairro), Clean(dto.EnderecoEmpresaCidade), Clean(dto.EnderecoEmpresaEstado)?.ToUpperInvariant() }
            .Where(x => x is not null));
        var parts = new[] { street, Clean(dto.EnderecoEmpresaComplemento), locality, Clean(dto.EnderecoEmpresaCep) }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        var formatted = string.Join(", ", parts);
        return Clean(formatted) ?? Clean(dto.EnderecoEmpresa);
    }
}
