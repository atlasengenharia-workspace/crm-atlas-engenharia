# Application Core

Centro da aplicação e independente de UI, banco de dados e integrações.

Tipos previstos pelo padrão adotado:

- `Entities`: entidades de negócio persistidas.
- `Aggregates`: raízes e limites de consistência.
- `Interfaces`: abstrações implementadas por Infrastructure.
- `Services`: serviços de domínio.
- `Specifications`: consultas e regras reutilizáveis.
- `Events`: eventos e manipuladores de domínio.
- `Exceptions`: exceções de negócio e guard clauses.

Este projeto não deve referenciar `Infrastructure` nem `Web`.
