using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.ApplicationCore.Sistema;

namespace CrmAtlas.UnitTests;

public sealed class ApplicationUseCaseTests
{
    [Fact]
    public async Task ClienteService_RejectsDuplicateDocument()
    {
        var repository = new MemoryRepository<Cliente>(
        [
            new Cliente
            {
                Id = 1,
                CnpjCpf = "12345678901",
                RazaoSocial = "Cliente existente"
            }
        ]);
        var cache = new MemoryCrmCache();
        var service = new ClienteService(repository, cache);
        var dto = new ClienteDto(
            null, "12345678901", "Novo cliente", null, null, null,
            null, null, null, null, null, null, null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task CustoIndiretoService_FiltersAndPaginates()
    {
        var repository = new MemoryRepository<CustoIndireto>(
        [
            new CustoIndireto
            {
                Id = 1,
                Data = new DateOnly(2026, 7, 1),
                Descricao = "Aluguel",
                Categoria = "Administrativo",
                Valor = 1000
            },
            new CustoIndireto
            {
                Id = 2,
                Data = new DateOnly(2026, 7, 2),
                Descricao = "Combustível",
                Categoria = "Operacional",
                Valor = 200
            }
        ]);
        var service = new CustoIndiretoService(repository, new MemoryRegistroHistoricoService());

        var result = await service.ListAsync(new(
            null, null, null, "administrativo", Page: 1, PageSize: 10));

        Assert.Single(result.Items);
        Assert.Equal("Aluguel", result.Items[0].Descricao);
        Assert.False(result.HasNext);
    }

    [Theory]
    [InlineData("Licenciamento", 1)]
    [InlineData("SRV-002", 2)]
    public async Task LancamentoService_PageSearchMatchesDescriptionOrServiceCode(
        string search,
        long expectedId)
    {
        var repository = new MemoryRepository<Lancamento>(
        [
            new Lancamento
            {
                Id = 1,
                Descricao = "Taxa de licenciamento",
                CodigoServico = "SRV-001",
                Data = new DateOnly(2026, 7, 1),
                Valor = 100,
                Tipo = LancamentoTipo.SAIDA
            },
            new Lancamento
            {
                Id = 2,
                Descricao = "Honorários",
                CodigoServico = "SRV-002",
                Data = new DateOnly(2026, 7, 2),
                Valor = 200,
                Tipo = LancamentoTipo.ENTRADA
            }
        ]);
        var service = new LancamentoService(
            repository,
            new MemoryRepository<CadastroServico>(),
            new MemoryRepository<Prestador>(),
            new MemoryRegistroHistoricoService());

        var result = await service.ListAsync(new(
            null, null, null, null, search, search, Page: 1, PageSize: 20));

        var item = Assert.Single(result.Items);
        Assert.Equal(expectedId, item.Id);
    }

    [Fact]
    public async Task ServicoTipoConfigService_ListAsyncMergesDefaultsWhenEmpty()
    {
        var service = new ServicoTipoConfigService(
            new MemoryRepository<ServicoTipoConfig>(), new MemoryCrmCache());

        var result = await service.ListAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal(Enum.GetValues<AcompanhamentoServicoTipo>().Length, result.Count);
        Assert.All(result, x => Assert.True(x.Ativo));
        Assert.Equal("Processos administrativos",
            result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.PROCESSOS_ADM).Label);
    }

    [Fact]
    public async Task ServicoTipoConfigService_ListAsyncAppliesSavedOverridesAndOrder()
    {
        var repository = new MemoryRepository<ServicoTipoConfig>(
        [
            new ServicoTipoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "AVCB / Licenças", Ordem = 3, Ativo = true },
            new ServicoTipoConfig { Id = 2, TipoServico = AcompanhamentoServicoTipo.CLCB, Ordem = 0, Ativo = false }
        ]);
        var service = new ServicoTipoConfigService(repository, new MemoryCrmCache());

        var result = await service.ListAsync();

        Assert.Equal(AcompanhamentoServicoTipo.CLCB, result[0].TipoServico);
        Assert.False(result[0].Ativo);
        Assert.Equal("AVCB / Licenças", result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB).Label);
        Assert.Equal("CLCB", result[0].Label);

        var active = await service.ListActiveAsync();
        Assert.DoesNotContain(active, x => x.TipoServico == AcompanhamentoServicoTipo.CLCB);
    }

    [Fact]
    public async Task ServicoTipoConfigService_SaveAsyncRejectsAllInactive()
    {
        var service = new ServicoTipoConfigService(
            new MemoryRepository<ServicoTipoConfig>(), new MemoryCrmCache());
        var configs = Enum.GetValues<AcompanhamentoServicoTipo>()
            .Select((tipo, index) => new ServicoTipoConfigDto(null, tipo, null, index, false))
            .ToList();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(configs));

        Assert.Contains("ativo", exception.Message);
    }

    [Fact]
    public async Task ServicoTipoConfigService_SaveAsyncPersistsAndTrimsNome()
    {
        var repository = new MemoryRepository<ServicoTipoConfig>();
        var cache = new MemoryCrmCache();
        var service = new ServicoTipoConfigService(repository, cache);
        var configs = new List<ServicoTipoConfigDto>
        {
            new(null, AcompanhamentoServicoTipo.AVCB, "  Laudos de incêndio  ", 0, true)
        };

        await service.SaveAsync(configs);
        var result = await service.ListAsync();

        var avcb = result.Single(x => x.TipoServico == AcompanhamentoServicoTipo.AVCB);
        Assert.Equal("Laudos de incêndio", avcb.Label);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task FormaPagamentoService_RejectsDuplicateNome()
    {
        var repository = new MemoryRepository<FormaPagamento>(
        [
            new FormaPagamento { Id = 1, Nome = "PIX" }
        ]);
        var service = new FormaPagamentoService(repository, new MemoryCrmCache());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(new FormaPagamentoDto(null, " pix ")));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task FormaPagamentoService_RequiresNome()
    {
        var service = new FormaPagamentoService(
            new MemoryRepository<FormaPagamento>(), new MemoryCrmCache());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(new FormaPagamentoDto(null, "   ")));

        Assert.Contains("obrigatório", exception.Message);
    }

    [Fact]
    public async Task FormaPagamentoService_CreateTrimsAndPersists()
    {
        var repository = new MemoryRepository<FormaPagamento>();
        var service = new FormaPagamentoService(repository, new MemoryCrmCache());

        var created = await service.CreateAsync(new FormaPagamentoDto(null, "  Dinheiro  "));

        Assert.Equal("Dinheiro", created.Nome);
        Assert.Single(repository.AsQueryable());
    }

    [Fact]
    public async Task OrcamentoSituacaoService_RejectsDuplicateNome()
    {
        var repository = new MemoryRepository<OrcamentoSituacao>(
            [new OrcamentoSituacao { Id = 1, Label = "Em análise" }]);
        var service = new OrcamentoSituacaoService(repository, new MemoryConfiguracaoHistoricoService(), new MemoryCrmCache());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(new OrcamentoSituacaoDto(null, " em análise ", 1, false, true, false, null)));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task OrcamentoSituacaoService_PadraoClearsOthers()
    {
        var repository = new MemoryRepository<OrcamentoSituacao>(
        [
            new OrcamentoSituacao { Id = 1, Label = "Em análise", Padrao = true, Ordem = 0 },
            new OrcamentoSituacao { Id = 2, Label = "Aprovado", Closed = true, Ordem = 1 }
        ]);
        var service = new OrcamentoSituacaoService(repository, new MemoryConfiguracaoHistoricoService(), new MemoryCrmCache());

        await service.SaveAsync(new OrcamentoSituacaoDto(2, "Aprovado", 1, true, true, true, null));

        Assert.False(repository.AsQueryable().Single(x => x.Id == 1).Padrao);
        Assert.True(repository.AsQueryable().Single(x => x.Id == 2).Padrao);
    }

    [Fact]
    public async Task OrcamentoSituacaoService_SaveRegistersHistorico()
    {
        var historico = new MemoryConfiguracaoHistoricoService();
        var repository = new MemoryRepository<OrcamentoSituacao>();
        var service = new OrcamentoSituacaoService(repository, historico, new MemoryCrmCache());

        await service.SaveAsync(new OrcamentoSituacaoDto(null, "Em revisão", 4, false, true, false, "#FF00FF"));
        await service.SaveAsync(new OrcamentoSituacaoDto(1, "Em revisão final", 4, false, true, false, "#FF00FF"));

        Assert.Equal(2, historico.Entries.Count);
        Assert.Equal("Criada", historico.Entries[0].Acao);
        Assert.Equal("Atualizada", historico.Entries[1].Acao);
        Assert.Contains("Nome", historico.Entries[1].Detalhes);
        Assert.Equal(ConfiguracaoContexto.SituacaoOrcamento, historico.Entries[0].Contexto);
    }

    [Fact]
    public async Task ConfiguracaoHistoricoService_RegistraResponsavel()
    {
        var repository = new MemoryRepository<ConfiguracaoHistorico>();
        var service = new ConfiguracaoHistoricoService(repository, new StubUserAccessor("Vinicius"));

        await service.RegistrarAsync(ConfiguracaoContexto.SituacaoAcompanhamento, "AVCB", 5, "Concluído", "Atualizada", "Cor: #FFF → #000");
        var result = await service.ListAsync(ConfiguracaoContexto.SituacaoAcompanhamento);

        var entry = Assert.Single(result);
        Assert.Equal("Vinicius", entry.ResponsavelNome);
        Assert.Equal("Concluído", entry.RegistroNome);
        Assert.Equal("AVCB", entry.Escopo);
    }

    [Fact]
    public void ConfiguracaoDiff_ReportsOnlyChangedFields()
    {
        var diff = ConfiguracaoDiff.Between(
            ("Nome", "Em análise", "Em revisão"),
            ("Ativa", "Sim", "Sim"),
            ("Cor", null, "#FF0000"));

        Assert.Equal("Nome: Em análise → Em revisão; Cor: — → #FF0000", diff);
    }

    [Fact]
    public async Task AcompanhamentoService_SaveSituationAsync_RejectsDuplicateNomePerTipo()
    {
        var repository = new MemoryAcompanhamentoRepository(
            [new AcompanhamentoServicoSituacaoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "Em análise" }]);
        var service = new AcompanhamentoService(repository, new MemoryConfiguracaoHistoricoService(), new MemoryRegistroHistoricoService());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveSituationAsync(new SituacaoConfigDto(null, AcompanhamentoServicoTipo.AVCB, " em análise", 1, false, true, [], null)));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task AcompanhamentoService_SaveSituationAsync_InicialClearsOthersAndAudits()
    {
        var repository = new MemoryAcompanhamentoRepository(
        [
            new AcompanhamentoServicoSituacaoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "Em análise", SituacaoInicial = true },
            new AcompanhamentoServicoSituacaoConfig { Id = 2, TipoServico = AcompanhamentoServicoTipo.CLCB, Nome = "Em análise", SituacaoInicial = true }
        ]);
        var historico = new MemoryConfiguracaoHistoricoService();
        var service = new AcompanhamentoService(repository, historico, new MemoryRegistroHistoricoService());

        await service.SaveSituationAsync(new SituacaoConfigDto(null, AcompanhamentoServicoTipo.AVCB, "Triagem", 0, true, true, [], "#123456"));

        Assert.False(repository.Situacoes.Single(x => x.Id == 1).SituacaoInicial);
        Assert.True(repository.Situacoes.Single(x => x.Id == 2).SituacaoInicial);
        Assert.True(repository.Situacoes.Single(x => x.Nome == "Triagem").SituacaoInicial);
        var entry = Assert.Single(historico.Entries);
        Assert.Equal("Criada", entry.Acao);
        Assert.Equal("AVCB", entry.Escopo);
    }

    [Fact]
    public async Task AcompanhamentoService_SaveSituationAsync_UpdateAuditsDiff()
    {
        var repository = new MemoryAcompanhamentoRepository(
            [new AcompanhamentoServicoSituacaoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "Em análise", Ativo = true }]);
        var historico = new MemoryConfiguracaoHistoricoService();
        var service = new AcompanhamentoService(repository, historico, new MemoryRegistroHistoricoService());

        await service.SaveSituationAsync(new SituacaoConfigDto(1, AcompanhamentoServicoTipo.AVCB, "Em análise", 0, false, false, [], null));

        var entry = Assert.Single(historico.Entries);
        Assert.Equal("Atualizada", entry.Acao);
        Assert.Contains("Ativa: Sim → Não", entry.Detalhes);
    }

    [Fact]
    public async Task AcompanhamentoService_DeleteSituationAsync_RemovesAndAudits()
    {
        var repository = new MemoryAcompanhamentoRepository(
            [new AcompanhamentoServicoSituacaoConfig { Id = 1, TipoServico = AcompanhamentoServicoTipo.AVCB, Nome = "Em análise" }]);
        var historico = new MemoryConfiguracaoHistoricoService();
        var service = new AcompanhamentoService(repository, historico, new MemoryRegistroHistoricoService());

        await service.DeleteSituationAsync(1);

        Assert.Empty(repository.Situacoes);
        var entry = Assert.Single(historico.Entries);
        Assert.Equal("Excluída", entry.Acao);
        Assert.Equal("Em análise", entry.RegistroNome);
    }

    [Fact]
    public async Task RegistroHistoricoService_RegistraAntesDepoisEResponsavel()
    {
        var repository = new MemoryRepository<RegistroHistorico>();
        var service = new RegistroHistoricoService(repository, new StubUserAccessor("Vinicius"));

        await service.RegistrarAsync(RegistroEntidade.Lancamento, 7, "L-000007",
            [("Valor", "R$ 100,00", "R$ 250,00"), ("Situação", "PREVISTO", "PAGO")]);

        var result = await service.ListAsync(RegistroEntidade.Lancamento, 7);
        Assert.Equal(2, result.Count);
        var valor = result.Single(x => x.Campo == "Valor");
        Assert.Equal("R$ 100,00", valor.ValorAnterior);
        Assert.Equal("R$ 250,00", valor.ValorNovo);
        Assert.Equal("Vinicius", valor.ResponsavelNome);
        Assert.Equal("L-000007", valor.EntidadeCodigo);
    }

    [Fact]
    public async Task RegistroHistoricoService_IgnoraCamposSemMudanca()
    {
        var repository = new MemoryRepository<RegistroHistorico>();
        var service = new RegistroHistoricoService(repository, new StubUserAccessor("Vinicius"));

        await service.RegistrarAsync(RegistroEntidade.Prestador, 3, "João",
            [("Nome", "João", "João"), ("Telefone", null, "  "), ("E-mail", null, "a@b.com")]);

        var result = await service.ListAsync(RegistroEntidade.Prestador, 3);
        var entry = Assert.Single(result);
        Assert.Equal("E-mail", entry.Campo);
    }

    [Fact]
    public async Task LancamentoService_UpdateAsync_AuditaValorESituacao()
    {
        var repository = new MemoryRepository<Lancamento>(
        [
            new Lancamento
            {
                Id = 1, Codigo = "L-000001", Descricao = "Honorários",
                Tipo = LancamentoTipo.ENTRADA, Status = LancamentoStatus.PREVISTO,
                Data = new DateOnly(2026, 7, 2), Valor = 200
            }
        ]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new LancamentoService(
            repository,
            new MemoryRepository<CadastroServico>(),
            new MemoryRepository<Prestador>(),
            historico);

        await service.UpdateAsync(1, new LancamentoDto(
            1, "L-000001", LancamentoTipo.ENTRADA, LancamentoStatus.PAGO, LancamentoOrigem.MANUAL,
            null, "SRV-001", null, null, null, "Honorários", new DateOnly(2026, 7, 2), 250,
            null, null, "PIX", null, null, null, null, null, "pago em dia",
            0, 0, 0, null, null));

        var valor = historico.Entries.Single(x => x.Campo == "Valor");
        Assert.Equal(RegistroHistoricoFormat.Money(200), valor.Antes);
        Assert.Equal(RegistroHistoricoFormat.Money(250), valor.Depois);
        Assert.Contains(historico.Entries, x => x.Campo == "Situação" && x.Antes == "PREVISTO" && x.Depois == "PAGO");
        Assert.Contains(historico.Entries, x => x.Campo == "Observação" && x.Depois == "pago em dia");
        Assert.All(historico.Entries, x => Assert.Equal("L-000001", x.Codigo));
    }

    [Fact]
    public async Task PrestadorService_SaveAsync_AuditaCamposAlterados()
    {
        var repository = new MemoryRepository<Prestador>(
            [new Prestador { Id = 1, Nome = "João", MetodoPagamento = "Boleto" }]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new PrestadorService(
            repository,
            new MemoryRepository<CadastroServico>(),
            new MemoryRepository<Lancamento>(),
            historico,
            new MemoryCrmCache());

        await service.SaveAsync(new PrestadorDto(1, "João Silva", null, null, null, "PIX", "joao@pix.com", null, null, null));

        Assert.Contains(historico.Entries, x => x.Campo == "Nome" && x.Antes == "João" && x.Depois == "João Silva");
        Assert.Contains(historico.Entries, x => x.Campo == "Método de pagamento" && x.Antes == "Boleto" && x.Depois == "PIX");
        Assert.Contains(historico.Entries, x => x.Campo == "Chave PIX" && x.Depois == "joao@pix.com");
        Assert.DoesNotContain(historico.Entries, x => x.Campo == "Banco");
    }

    [Fact]
    public async Task AcompanhamentoService_UpdateDescricaoAsync_AuditaObservacao()
    {
        var repository = new MemoryAcompanhamentoRepository(
            itens: [new AcompanhamentoServico { Id = 5, Codigo = "S-AVCB-0001", Situacao = "Em andamento", Descricao = "nota antiga" }]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new AcompanhamentoService(repository, new MemoryConfiguracaoHistoricoService(), historico);

        await service.UpdateDescricaoAsync(5, "nota nova");

        var entry = Assert.Single(historico.Entries);
        Assert.Equal("Observação", entry.Campo);
        Assert.Equal("nota antiga", entry.Antes);
        Assert.Equal("nota nova", entry.Depois);
        Assert.Equal("S-AVCB-0001", entry.Codigo);
    }

    [Fact]
    public async Task GlobalSearchService_EncontraPorDocumentoSemPontuacaoEPorObservacao()
    {
        var prestadores = new MemoryRepository<Prestador>(
            [new Prestador { Id = 1, Nome = "João", CnpjCpf = "12.345.678/0001-90" }]);
        var lancamentos = new MemoryRepository<Lancamento>(
        [
            new Lancamento
            {
                Id = 2, Codigo = "L-000002", Descricao = "Honorários",
                Tipo = LancamentoTipo.ENTRADA, Status = LancamentoStatus.PAGO,
                Data = new DateOnly(2026, 7, 2), Valor = 300,
                Observacao = "nota fiscal emitida pela prefeitura"
            }
        ]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new GlobalSearchService(
            new ClienteService(new MemoryRepository<Cliente>(), new MemoryCrmCache()),
            new StubCadastroServicoService([]),
            new OrcamentoService(
                new MemoryRepository<Orcamento>(),
                new MemoryRepository<OrcamentoSituacao>(),
                new MemoryRepository<OrcamentoHistorico>(),
                new StubUserAccessor(null),
                historico),
            new PrestadorService(prestadores, new MemoryRepository<CadastroServico>(), lancamentos, historico, new MemoryCrmCache()),
            new StubAcompanhamentoService([]),
            new LancamentoService(lancamentos, new MemoryRepository<CadastroServico>(), prestadores, historico));

        var porDocumento = await service.SearchAsync("12345678000190");
        Assert.Contains(porDocumento, x => x.Tipo == GlobalSearchResultType.PRESTADOR && x.Id == 1);

        var porObservacao = await service.SearchAsync("prefeitura");
        Assert.Contains(porObservacao, x => x.Tipo == GlobalSearchResultType.LANCAMENTO && x.Id == 2);
    }

    [Fact]
    public async Task AcompanhamentoService_UpdateFolderUrlAsync_SalvaAuditaELimpa()
    {
        var repository = new MemoryAcompanhamentoRepository(
            itens: [new AcompanhamentoServico { Id = 5, Codigo = "S-AVCB-0001", Situacao = "Em andamento" }]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new AcompanhamentoService(repository, new MemoryConfiguracaoHistoricoService(), historico);

        await service.UpdateFolderUrlAsync(5, "  https://drive.google.com/pasta  ");

        Assert.Equal("https://drive.google.com/pasta", repository.Itens[0].FolderUrl);
        var entry = Assert.Single(historico.Entries);
        Assert.Equal("Pasta no Drive", entry.Campo);
        Assert.Null(entry.Antes);
        Assert.Equal("https://drive.google.com/pasta", entry.Depois);
        Assert.Equal("S-AVCB-0001", entry.Codigo);

        await service.UpdateFolderUrlAsync(5, null);
        Assert.Null(repository.Itens[0].FolderUrl);
    }

    [Fact]
    public async Task CadastroServicoService_UpdateAsync_SalvaEAuditaPastaDrive()
    {
        var repository = new MemoryCadastroServicoRepository(
        [
            new CadastroServico
            {
                Id = 1, Codigo = "S-AVCB-0001", Subtipo = "Projeto",
                ValorContrato = 1000, DataContrato = new DateOnly(2026, 1, 1),
                FolderUrl = "https://drive.google.com/antiga"
            }
        ]);
        var historico = new MemoryRegistroHistoricoService();
        var service = new CadastroServicoService(
            repository,
            new MemoryRepository<Cliente>(),
            new MemoryRepository<Orcamento>(),
            new MemoryRepository<CondicaoPagamento>(),
            new MemoryRepository<Prestador>(),
            new MemoryRepository<OrcamentoHistorico>(),
            new MemoryRepository<Lancamento>(),
            new MemoryRepository<ServicoSubtipoConfig>(),
            new StubUserAccessor("Vinicius"),
            historico,
            new MemoryCrmCache());

        await service.UpdateAsync(1, new CadastroServicoDto(
            Id: 1, Codigo: "S-AVCB-0001", ClienteId: null, OrcamentoId: null, OrcamentoCodigo: null,
            CondicaoPagamentoId: null, TipoServico: AcompanhamentoServicoTipo.AVCB, Subtipo: "Projeto",
            DataEntrada: new DateOnly(2026, 1, 1), SituacaoInicial: null, DocumentoEmpresa: null,
            RazaoSocialEmpresa: "Empresa X", ContatoEmpresa: null, Telefone: null, Email: null,
            EnderecoEmpresa: null, EnderecoEmpresaRua: null, EnderecoEmpresaNumero: null,
            EnderecoEmpresaBairro: null, EnderecoEmpresaComplemento: null, EnderecoEmpresaCidade: null,
            EnderecoEmpresaEstado: null, EnderecoEmpresaCep: null, EnderecoServico: null,
            EnderecoServicoRua: null, EnderecoServicoNumero: null, EnderecoServicoBairro: null,
            EnderecoServicoComplemento: null, EnderecoServicoCidade: null, EnderecoServicoEstado: null,
            EnderecoServicoCep: null, MesmoEnderecoEmpresa: false,
            ValorContrato: 1000, DataContrato: new DateOnly(2026, 1, 1), NomeCondicaoPagamento: null,
            ValorNotaFiscal: null, ValorNotaFiscalDividido: false, ValorNotaFiscalParcela: null,
            Observacao: null,
            Parcelas: [new CadastroServicoParcelaDto(null, 1, 1000, null, null)],
            Prestadores: [], CreatedAt: null,
            FolderUrl: "https://drive.google.com/nova"));

        Assert.Equal("https://drive.google.com/nova", repository.Items[0].FolderUrl);
        var entry = Assert.Single(historico.Entries, x => x.Campo == "Pasta no Drive");
        Assert.Equal("https://drive.google.com/antiga", entry.Antes);
        Assert.Equal("https://drive.google.com/nova", entry.Depois);
        Assert.Equal("S-AVCB-0001", entry.Codigo);
    }

    private sealed class StubCadastroServicoService(IReadOnlyList<CadastroServicoDto> items) : ICadastroServicoService
    {
        public Task<PagedResult<CadastroServicoDto>> ListAsync(CadastroServicoFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<CadastroServicoDto>.Create(items.ToList(), 1, items.Count, items.Count));
        public Task<IReadOnlyList<CadastroServicoSubtipoConfigDto>> ListSubtiposAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ServicoSubtipoConfigItemDto>> ListSubtipoConfigsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveSubtiposAsync(AcompanhamentoServicoTipo tipo, IReadOnlyList<string> nomes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CadastroServicoDto> GetAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CadastroServicoDto> CreateAsync(CadastroServicoDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CadastroServicoDto> UpdateAsync(long id, CadastroServicoDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAcompanhamentoService(IReadOnlyList<AcompanhamentoDto> items) : IAcompanhamentoService
    {
        public Task<PagedResult<AcompanhamentoDto>> ListAsync(AcompanhamentoFilter? filter = null, CancellationToken ct = default) =>
            Task.FromResult(PagedResult<AcompanhamentoDto>.Create(items.ToList(), 1, items.Count, items.Count));
        public Task<AcompanhamentoDto> GetAsync(long id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AcompanhamentoDto>> ImportAsync(IReadOnlyList<AcompanhamentoImportDto> rows, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ChangeStatusAsync(long id, string novaSituacao, string? descricao, string? responsavel, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateDescricaoAsync(long id, string? descricao, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateFolderUrlAsync(long id, string? url, CancellationToken ct = default) => throw new NotSupportedException();
        public Task BulkUpdateAsync(IReadOnlyList<long> ids, string? situacao, string? descricao, string? responsavel, CancellationToken ct = default) => throw new NotSupportedException();
        public Task TogglePendingAsync(long serviceId, long pendingId, bool completed, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SituacaoConfigDto>> ListSituationsAsync(AcompanhamentoServicoTipo? tipo = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SituacaoConfigDto> SaveSituationAsync(SituacaoConfigDto dto, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteSituationAsync(long id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(long id, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task ClienteService_RejectsDuplicateDocumentIgnoringFormatting()
    {
        var repository = new MemoryRepository<Cliente>(
            [new Cliente { Id = 1, CnpjCpf = "12.345.678/0001-90", RazaoSocial = "Existente" }]);
        var service = new ClienteService(repository, new MemoryCrmCache());
        var dto = new ClienteDto(
            null, "12345678000190", "Outro nome", null, null, null,
            null, null, null, null, null, null, null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));

        Assert.Contains("Já existe", exception.Message);
    }

    [Fact]
    public async Task ClienteService_FindDuplicatesAsync_EncontraTelefoneEIgualaProprioId()
    {
        var repository = new MemoryRepository<Cliente>(
        [
            new Cliente { Id = 1, CnpjCpf = "12345678901", RazaoSocial = "Maria", Telefone = "(11) 99988-7766" },
            new Cliente { Id = 2, CnpjCpf = "98765432100", RazaoSocial = "João", Telefone = "11999887766" }
        ]);
        var service = new ClienteService(repository, new MemoryCrmCache());

        var porTelefone = await service.FindDuplicatesAsync(null, null, "11 99988-7766");
        Assert.Equal(2, porTelefone.Count);
        Assert.All(porTelefone, x => Assert.Equal("mesmo telefone", x.Motivo));

        var excluindoProprio = await service.FindDuplicatesAsync(1, "123.456.789-01", "(11) 99988-7766");
        var match = Assert.Single(excluindoProprio);
        Assert.Equal(2, match.Id);
        Assert.Equal("mesmo telefone", match.Motivo);
    }

    [Fact]
    public async Task PrestadorService_FindDuplicatesAsync_EncontraDocumentoETelefone()
    {
        var repository = new MemoryRepository<Prestador>(
        [
            new Prestador { Id = 1, Nome = "Pedro", CnpjCpf = "12.345.678/0001-90" },
            new Prestador { Id = 2, Nome = "Ana", Telefone = "(21) 98877-6655" }
        ]);
        var service = new PrestadorService(
            repository,
            new MemoryRepository<CadastroServico>(),
            new MemoryRepository<Lancamento>(),
            new MemoryRegistroHistoricoService(),
            new MemoryCrmCache());

        var porDoc = await service.FindDuplicatesAsync(null, "12345678000190", null);
        var doc = Assert.Single(porDoc);
        Assert.Equal(1, doc.Id);
        Assert.Equal("mesmo CPF/CNPJ", doc.Motivo);

        var porFone = await service.FindDuplicatesAsync(null, null, "21988776655");
        Assert.Single(porFone, x => x.Id == 2);
    }

    private sealed class MemoryRegistroHistoricoService : IRegistroHistoricoService
    {
        public List<(string Entidade, long EntidadeId, string? Codigo, string Campo, string? Antes, string? Depois)> Entries { get; } = [];

        public Task RegistrarAsync(string entidade, long entidadeId, string? entidadeCodigo,
            IEnumerable<(string Campo, string? Antes, string? Depois)> campos, CancellationToken ct = default)
        {
            foreach (var (campo, antes, depois) in campos)
            {
                var a = string.IsNullOrWhiteSpace(antes) ? null : antes.Trim();
                var d = string.IsNullOrWhiteSpace(depois) ? null : depois.Trim();
                if (!string.Equals(a, d, StringComparison.Ordinal))
                    Entries.Add((entidade, entidadeId, entidadeCodigo, campo, antes, depois));
            }
            return Task.CompletedTask;
        }

        public Task RegistrarEventoAsync(string entidade, long entidadeId, string? entidadeCodigo, string evento, CancellationToken ct = default)
        {
            Entries.Add((entidade, entidadeId, entidadeCodigo, "Registro", null, evento));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RegistroHistoricoDto>> ListAsync(string? entidade = null, long? entidadeId = null, int take = 200, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RegistroHistoricoDto>>([]);
    }

    private sealed class StubUserAccessor(string? name) : IUserAccessor
    {
        public Task<string?> GetUserNameAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(name);
    }

    private sealed class MemoryConfiguracaoHistoricoService : IConfiguracaoHistoricoService
    {
        public List<(string Contexto, string? Escopo, long? RegistroId, string RegistroNome, string Acao, string? Detalhes)> Entries { get; } = [];

        public Task RegistrarAsync(string contexto, string? escopo, long? registroId, string registroNome, string acao, string? detalhes = null, CancellationToken cancellationToken = default)
        {
            Entries.Add((contexto, escopo, registroId, registroNome, acao, detalhes));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConfiguracaoHistoricoDto>> ListAsync(string? contexto = null, int take = 200, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConfiguracaoHistoricoDto>>([]);
    }

    private sealed class MemoryAcompanhamentoRepository(
        IEnumerable<AcompanhamentoServicoSituacaoConfig>? situacoes = null,
        IEnumerable<AcompanhamentoServico>? itens = null)
        : IAcompanhamentoRepository
    {
        public List<AcompanhamentoServicoSituacaoConfig> Situacoes { get; } = situacoes?.ToList() ?? [];
        public List<AcompanhamentoServico> Itens { get; } = itens?.ToList() ?? [];

        public Task<IReadOnlyList<AcompanhamentoServicoSituacaoConfig>> ListSituationsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AcompanhamentoServicoSituacaoConfig>>(Situacoes.ToList());

        public Task<AcompanhamentoServicoSituacaoConfig?> GetSituationAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(Situacoes.FirstOrDefault(x => x.Id == id));

        public Task AddSituationAsync(AcompanhamentoServicoSituacaoConfig entity, CancellationToken ct = default)
        {
            entity.Id = Situacoes.Count == 0 ? 1 : Situacoes.Max(x => x.Id) + 1;
            Situacoes.Add(entity);
            return Task.CompletedTask;
        }

        public void UpdateSituation(AcompanhamentoServicoSituacaoConfig entity) { }
        public void RemoveSituation(AcompanhamentoServicoSituacaoConfig entity) => Situacoes.Remove(entity);
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<IReadOnlyList<AcompanhamentoServico>> ListDetailedAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AcompanhamentoServico>>(Itens.ToList());
        public IQueryable<AcompanhamentoServico> AsQueryable() => Itens.AsQueryable();
        public Task<IReadOnlyList<AcompanhamentoServico>> ToListAsync(IQueryable<AcompanhamentoServico> query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AcompanhamentoServico>>(query.ToList());
        public Task<int> CountAsync(IQueryable<AcompanhamentoServico> query, CancellationToken ct = default) =>
            Task.FromResult(query.Count());
        public Task<AcompanhamentoServico?> GetDetailedAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task AddAsync(AcompanhamentoServico entity, CancellationToken ct = default)
        {
            entity.Id = Itens.Count == 0 ? 1 : Itens.Max(x => x.Id) + 1;
            Itens.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(AcompanhamentoServico entity) { }
        public void Remove(AcompanhamentoServico entity) => Itens.Remove(entity);
    }

    private sealed class MemoryCadastroServicoRepository(IEnumerable<CadastroServico>? seed = null)
        : ICadastroServicoRepository
    {
        public List<CadastroServico> Items { get; } = seed?.ToList() ?? [];

        public Task<CadastroServico?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<CadastroServico?> FindAsync(System.Linq.Expressions.Expression<Func<CadastroServico, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));
        public Task<IReadOnlyList<CadastroServico>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CadastroServico>>(Items.ToList());
        public IQueryable<CadastroServico> AsQueryable() => Items.AsQueryable();
        public Task<IReadOnlyList<CadastroServico>> ToListAsync(IQueryable<CadastroServico> query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CadastroServico>>(query.ToList());
        public Task<int> CountAsync(IQueryable<CadastroServico> query, CancellationToken cancellationToken = default) =>
            Task.FromResult(query.Count());
        public Task AddAsync(CadastroServico entity, CancellationToken cancellationToken = default)
        {
            entity.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
            Items.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(CadastroServico entity) { }
        public void Remove(CadastroServico entity) => Items.Remove(entity);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
        public IQueryable<CadastroServico> AsNoTrackingDetailed() => Items.AsQueryable();
        public Task<IReadOnlyList<CadastroServico>> ListDetailedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CadastroServico>>(Items.ToList());
        public Task<CadastroServico?> GetDetailedAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    }

    private sealed class MemoryCrmCache : ICrmCache
    {
        private readonly Dictionary<string, object> _store = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult(value as T);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryRepository<TEntity>(IEnumerable<TEntity>? seed = null)
        : IRepository<TEntity> where TEntity : Entity
    {
        private readonly List<TEntity> _items = seed?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<TEntity?> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().FirstOrDefault(predicate));

        public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TEntity>>(_items.ToList());

        public IQueryable<TEntity> AsQueryable() => _items.AsQueryable();

        public Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TEntity>>(query.ToList());

        public Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
            Task.FromResult(query.Count());

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity) { }
        public void Remove(TEntity entity) => _items.Remove(entity);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
