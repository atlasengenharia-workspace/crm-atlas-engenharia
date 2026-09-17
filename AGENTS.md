# CRM Atlas Engenharia — regras do projeto

## Regra de entrega (SISTEMA-14)

Antes de marcar uma task como concluída, **enumerar e verificar todas as telas relacionadas** ao dado/comportamento alterado. Uma melhoria entregue numa tela e ausente nas demais é entrega incompleta.

Checklist obrigatório quando a mudança tocar dado compartilhado:

- **Campo de serviço** → `/acompanhamento` (grid + drawer + "editar cadastro completo" via `ServicoForm`), `/cadastros-servico` (grid + form inline — lembrar que `CadastrosServico.razor` tem `ServiceModel`/`ToDto` próprios, duplicados do `ServicoForm`), busca global `/busca`, relatórios PDF/Excel.
- **Catálogo** (tipos, situações, formas de pagamento, condições) → todo select/filtro/chip que exibe o valor, incluindo telas de listagem e dashboards; valores legados fora do catálogo devem continuar visíveis.
- **AcompanhamentoServico ↔ CadastroServico** são tabelas paralelas ligadas por `Codigo` — mudança num lado precisa propagar ou ser justificada.
- **Auditoria** → campo novo em entidade auditada entra no snapshot/diff (`registro_historico` + timeline quando fizer sentido).
- **Migrations** → após `dotnet ef migrations add`, verificar conflito com o SQL bruto do `Program.cs` (ele cria objetos sem migration; se uma migration falha ali, as seguintes não aplicam — o catch engole a exceção). Rodar `dotnet ef database update` e conferir `__EFMigrationsHistory`.

## Build e testes

```bash
dotnet build src/frontend/frontend/frontend.csproj   # build completo (0 warnings exigidos)
dotnet test tests/UnitTests/UnitTests.csproj
dotnet test tests/IntegrationTests/IntegrationTests.csproj
dotnet ef migrations add <Nome> --project src/Infrastructure --startup-project src/frontend/frontend
dotnet ef database update --project src/Infrastructure --startup-project src/frontend/frontend
```

## Convenções

- .NET 10, Blazor Server (MudBlazor 9 + Radzen), EF Core + PostgreSQL (Neon).
- Camadas: `ApplicationCore` (entidades/use cases/DTOs), `Infrastructure` (EF/repositórios), `frontend` (páginas/API).
- Auditoria genérica: `IRegistroHistoricoService.RegistrarAsync(entidade, id, codigo, [(campo, antes, depois)])` — filtra no-ops sozinho; responsável via `IUserAccessor`.
- Tabelas **sem** migration são criadas no SQL bruto do `Program.cs`; tabelas **com** migration nunca vão no SQL bruto.
- Preferências por máquina (ex.: raiz local do Drive) ficam em `localStorage` via `IJSRuntime`, não em preferências de usuário.
