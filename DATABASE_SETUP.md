# Banco de dados

A persistência usa EF Core 10 com PostgreSQL por meio do projeto
`Infrastructure`.

## Configuração

Não grave credenciais em `appsettings*.json`. Para a sessão local:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=HOST;Port=5432;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require"
dotnet run --project src/Web/Web.csproj --launch-profile https
```

Em produção, configure `ConnectionStrings__DefaultConnection` no cofre de
segredos da plataforma.

## Migration inicial

`InitialLegacySchema` representa o esquema migrado das entidades JPA:

- 29 tabelas;
- 18 chaves estrangeiras;
- 34 índices.

Para um banco vazio:

```powershell
dotnet ef database update `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Infrastructure/Infrastructure.csproj
```

Não execute essa migration diretamente sobre o banco Java já populado. Nesse
caso, compare primeiro o script gerado com o esquema existente e faça baseline
da migration:

```powershell
dotnet ef migrations script `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Infrastructure/Infrastructure.csproj `
  --output migration-review.sql
```

## Segurança

O projeto Java de origem contém uma credencial PostgreSQL versionada em arquivo
de configuração. Ela deve ser rotacionada no provedor e removida do histórico
do repositório antes de qualquer reutilização do banco.
