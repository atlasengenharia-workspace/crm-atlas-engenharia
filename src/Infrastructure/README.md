# Infrastructure

Implementa as interfaces definidas no `ApplicationCore`.

Tipos previstos pelo padrão adotado:

- `Data`: DbContext, mappings e migrations do EF Core.
- `Repositories`: implementações de persistência.
- `Identity`: adaptação de identidade quando houver regras fora da UI.
- `Services`: e-mail, arquivos, cache, filas e clientes de APIs.

O projeto pode referenciar `ApplicationCore`, mas nunca `Web`.
