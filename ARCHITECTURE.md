# Arquitetura do CRM Atlas

A solução segue a Clean Architecture apresentada em *Architecting Modern Web
Applications with ASP.NET Core and Azure*.

## Dependências

```text
Web --------------------> ApplicationCore
  \                         ^
   \----> Infrastructure ---|
```

- Dependências de compilação apontam para o núcleo.
- `ApplicationCore` não conhece detalhes de infraestrutura ou apresentação.
- `Infrastructure` implementa abstrações definidas pelo núcleo.
- `Web` é a UI e a composition root. A referência a `Infrastructure` deve ser
  usada somente na configuração de injeção de dependência.

## Projetos

| Projeto | Responsabilidade |
| --- | --- |
| `src/ApplicationCore` | Modelo de negócio, entidades, interfaces e serviços de domínio |
| `src/Infrastructure` | Persistência e integrações externas |
| `src/Web` | Blazor, Razor Pages, autenticação e composição da aplicação |
| `tests/UnitTests` | Testes isolados do Application Core e regras arquiteturais |
| `tests/IntegrationTests` | Testes de Infrastructure com dependências resolvidas |

Novas funcionalidades devem começar no modelo e nos casos de uso do
`ApplicationCore`, receber implementações externas em `Infrastructure` e ser
expostas ao usuário por `Web`.

dotnet run --project src/Web/Web.csproj --launch-profile https