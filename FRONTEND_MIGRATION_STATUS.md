# Migração do frontend — estado de execução

## Entregue nesta etapa

- Shell autenticado com busca global, tema claro/escuro, notificações e menu responsivo.
- Central de cadastros.
- Orçamentos e Prestadores com CRUD persistido e respectivos casos de uso em `ApplicationCore`.
- Clientes com indicadores reais, máscaras brasileiras e preenchimento de endereço por ViaCEP.
- Cadastro de serviço integrado a orçamentos e prestadores, com custos provisionados/efetivos e política de pagamento.
- Lançamentos integrados a serviço, parcela e prestador, com comprovantes armazenados fora da área pública e download autenticado.
- Custos indiretos com categorias sugeridas, indicadores, gráfico por categoria e importação CSV com pré-visualização/validação.
- Acompanhamento persistido com situações configuráveis, histórico, pendências concluíveis e visões unificadas por tipo.
- Importação de acompanhamentos com pré-visualização, validação e proteção contra origens duplicadas.
- Relatório PDF de acompanhamento gerado no servidor e disponibilizado por endpoint autenticado.
- Dashboard executivo com período filtrável, KPIs financeiros/operacionais e gráficos baseados em dados persistidos.
- Usuário local resolvido pelos claims Auth0 sem duplicação de conta.
- Notificações persistidas por usuário, com leitura, confirmação e regras idempotentes para parcelas e serviços parados.
- Busca global navegável para clientes, serviços, orçamentos, prestadores e acompanhamentos, com acesso desktop e móvel.
- Preferências persistidas por usuário Auth0: tema, densidade, menu, resumo por e-mail e alertas do navegador.
- Acompanhamento unificado e visões filtradas AVCB, CLCB, Obras e Processos.
- Caso de uso para mudança de situação com histórico.
- Central de notificações e caso de uso para leitura.
- Relatórios e templates, Preferências, Atlas IA e Gestão Ads.
- Navegação completa e estados visuais responsivos com MudBlazor.

## Fluxos que ainda exigem integração funcional

- Completar desconto de nota por parcela e geração PDF no cadastro de serviço.
- Importações Inter/Asaas e leitura automatizada de comprovantes em Lançamentos.
- Implementar geração real de PDF, regras agendadas de notificação e provedores de IA/Ads.
- Persistir preferências do shell por usuário.

## Validação

- `dotnet build src\Web\Web.csproj -c Release --no-restore`
- Resultado: 0 erros e 0 avisos.
- `dotnet test --no-restore -c Release`
- Resultado: 6 testes aprovados, 0 falhas.
- Após bloqueio transitório do Windows Application Control sobre o assembly Release dos testes de integração,
  a suíte de integração foi reexecutada em Debug: 3 aprovados, 0 falhas.
