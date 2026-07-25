namespace CrmAtlas.ApplicationCore.Sistema;

public sealed class SistemaAtualizacaoService : ISistemaAtualizacaoService
{
    private static readonly List<SistemaAtualizacao> History =
    [
        new(
            "v2.4.0",
            new DateOnly(2026, 7, 25),
            "Autenticação Auth0, Módulo de Atualizações & Design Responsivo Mobile/Tablet",
            "Atualização completa de segurança de rotas de autenticação, sistema interativo de notas de versão e suporte fluído para celulares (Android/iOS) e tablets.",
            CategoriaAtualizacao.NovoRecurso,
            [
                new("Rotas de Autenticação Auth0 Unificadas", "Suporte completo a /login, /logout, /signup e /auth0 com redirecionamento pt-BR e suporte a telas mobile."),
                new("Módulo de Atualizações do Sistema", "Linha do tempo interativa com histórico de versões, categorias de alterações e notificações visuais."),
                new("Interface Totalmente Responsiva", "Adaptabilidade completa para Android, iOS e tablets com barra lateral retrátil, botões de toque otimizados e tabelas flexíveis."),
                new("Otimização de Carregamentos", "Skeletons e estados de carregamento suaves em todas as páginas para navegação instantânea.")
            ],
            DestaquePrincipal: true
        ),
        new(
            "v2.3.5",
            new DateOnly(2026, 7, 25),
            "Suporte a CPF/CNPJ, Número/Código e Importação Inteligente de Planilhas",
            "Aprimoramento do leitor de planilhas com busca flexível de abas (Custos Indiretos, Lançamentos, AVCB, CLCB, Obras, Processos Adm) e atualização automática de CPFs/CNPJs.",
            CategoriaAtualizacao.Melhoria,
            [
                new("Leitura de CPF/CNPJ e Códigos", "Reconhecimento de variações de cabeçalhos (CPF/CNPJ, Doc, Número, Nº, Protocolo) em planilhas Excel e CSV."),
                new("Importação de Custos Indiretos e Lançamentos", "Suporte flexível a abas de 'custo indireto' e 'lançamentos' sem restrição de acentos ou maiúsculas."),
                new("Backfill Automático de Documentos", "Atualização inteligente dos 770 clientes legados substituindo identificadores temporários pelos CPFs/CNPJs reais.")
            ]
        ),
        new(
            "v2.3.0",
            new DateOnly(2026, 7, 24),
            "Exibição do Código de Clientes e Gaveteiro de Detalhes Operacionais",
            "Inclusão da coluna Código (#1, #2, ...) na listagem de Empresas e Pessoas e exibição estendida no gaveteiro de Acompanhamento.",
            CategoriaAtualizacao.Melhoria,
            [
                new("Coluna Código em Empresas e Pessoas", "Exibição clara e ordenável do ID/Código do cliente na tabela principal."),
                new("Gaveteiro Estendido de Acompanhamento", "Visualização direta de CPF/CNPJ, Endereço, Telefone, Nota Fiscal e Condição de Pagamento em drawer lateral.")
            ]
        ),
        new(
            "v2.2.0",
            new DateOnly(2026, 7, 23),
            "Painel Único de Acompanhamento de Serviços e Emissão de PDF",
            "Módulo de gestão operacional centralizado para AVCB, CLCB, Obras e Processos Administrativos com relatórios em PDF.",
            CategoriaAtualizacao.NovoRecurso,
            [
                new("Painel Unificado de Serviços", "Centralização dos tipos de serviço com troca rápida de situação e histórico de mudanças."),
                new("Relatórios em PDF", "Geração de relatórios operacionais formatados para impressão ou envio ao cliente.")
            ]
        )
    ];

    public Task<IReadOnlyList<SistemaAtualizacao>> GetListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SistemaAtualizacao>>(History);

    public Task<SistemaAtualizacao?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(History.FirstOrDefault());
}
