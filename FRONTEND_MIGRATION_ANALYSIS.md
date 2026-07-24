# Análise de migração do frontend legado

## Escopo

Comparação entre:

- frontend legado React 19 + Ant Design 6 em `C:\Users\Vinicius Moreira\Documents\GitHub\crm-atlas\frontend`;
- frontend novo Blazor Server + MudBlazor em `src\Web`;
- casos de uso disponíveis atualmente em `ApplicationCore`.

Foram avaliados inventário de rotas, navegação, formulários, tabelas, integrações, estados de tela,
responsividade e riscos de acessibilidade. A tela de login foi validada visualmente. As telas internas
foram analisadas pelo código porque a autenticação do frontend legado está bloqueada no Auth0.

## Resumo executivo

O frontend legado possui 36 declarações de rota e 25 páginas funcionais. O novo frontend possui dez
rotas Razor, das quais seis representam fluxos de negócio já migrados.

O novo frontend já cobre bem o primeiro recorte:

- visão geral básica;
- clientes;
- cadastro unificado de serviço;
- condições de pagamento;
- lançamentos;
- custos indiretos;
- perfil, autenticação e páginas de erro.

Ele ainda não possui paridade funcional com o legado. As maiores lacunas são acompanhamento de
serviços, cadastros de orçamento e prestadores, painéis especializados, importações, comprovantes,
relatórios PDF, notificações, dashboard executivo, busca global, IA e gestão de anúncios.

## Inventário e estado de migração

| Área legado | Rotas principais | Situação nova | Recomendação |
|---|---|---:|---|
| Autenticação | `/auth/login`, `/auth/callback` | Parcial | Manter Auth0 server-side; corrigir tenant/conexão e preservar identidade visual |
| Insights | `/` | Parcial | Evoluir o dashboard básico para filtros, KPIs e gráficos executivos |
| Clientes | `/gestao-de-clientes`, `/novo`, `/:id/editar` | Coberta | Acrescentar busca de CEP, máscaras e cards estatísticos |
| Cadastros | `/cadastros` | Ausente | Criar hub de acesso rápido ou substituir por navegação direta bem sinalizada |
| Orçamentos | `/cadastros/orcamentos` | Ausente | Portar entidade, casos de uso, situação configurável e conversão em serviço |
| Serviços/clientes | `/cadastros/servicos` | Parcial | Completar orçamento, desconto por parcela, prestadores e geração de pedido PDF |
| Prestadores | `/cadastros/prestadores` | Ausente | Portar CRUD, dados bancários e serviços vinculados |
| Acompanhamento | `/acompanhamento-servicos` | Ausente | Prioridade alta: situação, histórico, pendências, lote, relatório e configurações |
| AVCB | `/avcb`, `/novo`, `/:id/editar` | Ausente | Consolidar como visão filtrada do acompanhamento, evitando CRUD duplicado |
| CLCB | `/clcb`, `/novo`, `/:id/editar` | Ausente | Consolidar como visão filtrada do acompanhamento |
| Obras | `/obras`, `/novo`, `/:id/editar` | Ausente | Consolidar como visão filtrada do acompanhamento |
| Processos administrativos | `/processos`, `/novo`, `/:id/editar` | Ausente | Consolidar como visão filtrada do acompanhamento |
| Lançamentos | `/lancamentos`, `/novo`, `/:id/editar` | Parcial | Completar vínculo de serviço/parcela/prestador, importação e comprovantes |
| Custos indiretos | `/custos-indiretos`, `/novo`, `/:id/editar` | Parcial | Acrescentar gráfico, categorias predefinidas e importação por arquivo |
| Notificações | `/notificacoes` | Ausente | Portar central, regras, confirmação e execução manual |
| Perfil | `/profile`, `/profile/configuracoes` | Parcial | Adicionar preferências de UI e configurações disponíveis no legado |
| Atlas IA | `/atlas-ai` | Ausente | Migrar somente depois dos fluxos operacionais principais |
| Gestão Ads | `/gestao-ads` | Ausente | Tratar como bounded context separado e baixa prioridade para o CRM principal |

## Detalhes dos fluxos prioritários

### Clientes

O formulário legado contém CPF/CNPJ, razão social, contato, telefone, e-mail e endereço completo.
Também integra consulta de CEP e aplica máscaras brasileiras.

O novo formulário possui os mesmos campos essenciais, mas ainda precisa de:

- preenchimento automático via CEP;
- máscaras progressivas de CPF/CNPJ, telefone e CEP;
- validação visual equivalente à regra do domínio;
- indicadores de clientes, cidades e estados da listagem antiga.

### Cadastro unificado de serviço

Esta é a página mais complexa do legado, com 1.739 linhas e três áreas:

1. cliente e serviço;
2. financeiro;
3. prestadores e custos.

O novo frontend já implementa identificação, contrato e parcelas. Para paridade ainda faltam:

- selecionar e converter orçamento existente;
- endereço estruturado com CEP;
- regras de desconto da nota fiscal por primeira, todas ou parcelas específicas;
- vínculo e criação rápida de prestadores;
- valores provisionado e efetivo do prestador;
- data de pagamento a definir, no término ou específica;
- editor de template e geração do pedido de compra em PDF;
- listagem de cadastros recentes dentro do fluxo.

Recomendação arquitetônica: dividir em componentes menores (`DadosCliente`, `DadosServico`,
`PlanoFinanceiro`, `PrestadoresVinculados`) e manter o componente de página apenas como
orquestrador. As regras de cálculo continuam no `ApplicationCore`.

### Lançamentos

O legado possui:

- entrada e saída;
- status;
- vínculo com serviço, cliente, prestador e parcela;
- forma de pagamento, banco/plataforma e empresa;
- upload de comprovante;
- importação Inter/Asaas;
- detecção automática de formato;
- leitura de comprovante PDF;
- indicadores e filtros avançados.

O novo frontend cobre CRUD, tipo, status, data, valor, código, descrição e observação. A próxima versão
deve priorizar os seletores relacionais e comprovantes antes das importações.

### Custos indiretos

O novo CRUD cobre o núcleo. O legado adiciona categorias sugeridas, gráficos, filtros mais ricos e
importação em lote. Essas funções podem ser adicionadas sem alterar a entidade atual.

### Acompanhamento de serviços

É a maior lacuna operacional. A página antiga oferece:

- tabela unificada de AVCB, CLCB, Obras e Processos;
- filtros e importação em lote;
- edição em drawer;
- mudança de situação;
- histórico;
- pendências configuráveis e conclusão;
- configurações de situações;
- inspeção/vistoria;
- relatório e template PDF;
- exclusão individual e em lote.

Essa área deve ser implementada antes de reproduzir quatro painéis separados. Uma página MudBlazor
unificada, com filtros salvos e abas por tipo, reduz duplicação e mantém a experiência conhecida.

## Shell e sistema visual

O legado oferece recursos que ainda não existem no shell novo:

- busca global;
- drawer de notificações;
- modo claro/escuro;
- preferências persistidas;
- variações de sidebar para celular, tablet, TV e corporativo;
- atalho para Atlas IA;
- menu de usuário mais completo.

O shell MudBlazor novo é mais simples e consistente, uma boa base. A migração deve preservar a
hierarquia do menu por `Principal`, `Painéis e Gestão`, `Financeiro` e `Gestões`, mas evitar menus
profundos em telas pequenas.

## Riscos de UX e acessibilidade

### Confirmados visualmente

- A tela de login tem hierarquia clara, ação primária única e boa identidade visual.
- O texto secundário e detalhes sobre a imagem têm contraste potencialmente baixo; precisa ser
  medido contra WCAG 2.2 AA.
- A ação de login leva a uma página técnica do Auth0 em inglês, sem recuperação contextual para o
  usuário.

### Inferidos pelo código

- Há uso extensivo de cores hexadecimais e estilos inline, dificultando consistência de contraste.
- Várias páginas têm entre 600 e 2.368 linhas, aumentando risco de estados inconsistentes e foco mal
  gerenciado em modais/drawers.
- Ações somente com ícones precisam de `aria-label` e tooltip.
- Tabelas densas precisam de alternativa responsiva real, não apenas rolagem horizontal.
- Mudanças assíncronas, importações e upload precisam anunciar sucesso/erro para tecnologia assistiva.
- Modo escuro e preferências devem respeitar `prefers-color-scheme` e não depender apenas de cor.

Não foi possível confirmar navegação por teclado, ordem de foco, leitores de tela e contraste das áreas
autenticadas por causa do bloqueio do Auth0.

## Sequência recomendada

### Fase 1 - completar o núcleo já migrado

1. máscaras, CEP e refinamentos de Clientes;
2. prestadores e orçamentos;
3. cadastro de serviço completo;
4. seletores relacionais e comprovantes de Lançamentos;
5. categorias, gráfico e importação de Custos Indiretos.

### Fase 2 - operação

1. acompanhamento unificado;
2. situações, histórico e pendências;
3. visões filtradas AVCB, CLCB, Obras e Processos;
4. relatórios e templates PDF.

### Fase 3 - inteligência e produtividade

1. dashboard executivo;
2. notificações e regras;
3. busca global e preferências;
4. Atlas IA;
5. Gestão Ads como módulo independente.

## Decisões para a nova infraestrutura

- Páginas Razor permanecem em `Web`.
- DTOs, interfaces e regras ficam em `ApplicationCore`.
- EF Core, arquivos, integrações e implementações ficam em `Infrastructure`.
- Componentes Razor não acessam `DbContext`.
- Cálculos financeiros não ficam duplicados na UI.
- Painéis especializados reutilizam o mesmo caso de uso de acompanhamento.
- Importações devem ter etapa de pré-visualização e confirmação antes da persistência.
- Toda página deve definir loading, vazio, erro, sucesso e confirmação destrutiva.

