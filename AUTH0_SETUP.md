# Configuração do Auth0

A aplicação usa uma **Regular Web Application** do Auth0 e autenticação
server-side por cookie.

## 1. URLs da aplicação no Auth0

Configure a aplicação no painel do Auth0 com:

- Allowed Callback URLs:
  - `http://localhost:5044/callback`
  - `https://localhost:7117/callback`
- Allowed Logout URLs:
  - `http://localhost:5044`
  - `https://localhost:7117`
- Allowed Web Origins:
  - `http://localhost:5044`
  - `https://localhost:7117`

As URLs de produção devem ser adicionadas às mesmas listas antes do deploy.

### Conexão de identidade

Em **Authentication > Database**, crie ou selecione a conexão
`Username-Password-Authentication`. Na aba **Applications**, habilite
explicitamente o cliente utilizado pelo CRM Atlas.

Se utilizar Google, Microsoft ou outro provedor social, habilite a respectiva
conexão para o mesmo cliente. Sem pelo menos uma conexão habilitada, o Auth0
retorna `invalid_request: no connections enabled for the client`.

## 2. Credenciais locais

Não grave o Client Secret em `appsettings*.json`.

Execute:

```powershell
dotnet user-secrets --project src/Web/Web.csproj set "Auth0:Domain" "dev-mzsxv1eai51z5f0k.us.auth0.com"
dotnet user-secrets --project src/Web/Web.csproj set "Auth0:ClientId" "Xpq7Ai2b4kdtCbVF2yJbcEm9cTQihe4Q"
dotnet user-secrets --project src/Web/Web.csproj set "Auth0:ClientSecret" "BJXBG2wMbzWbc1yd1msetUD72rvuEtR14S3hVYtfLL7MfwoO3nmJDKhflQ9PuS7f"
```

O domínio deve ser informado sem o prefixo `https://`.

## 3. Produção

Use um cofre de segredos ou estas variáveis de ambiente:

```text
Auth0__Domain
Auth0__ClientId
Auth0__ClientSecret
```

## 4. Fluxos disponíveis

- `/Login`: inicia o Universal Login.
- `/Signup`: abre o Universal Login no fluxo de cadastro.
- `/Logout`: encerra a sessão do Auth0 e remove o cookie local.
- `/profile`: exibe o perfil do usuário autenticado.
- `/`: rota protegida; usuários anônimos são enviados para `/Login`.

O parâmetro `returnUrl` aceita somente URLs locais, evitando redirecionamentos
externos após o login.
