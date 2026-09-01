# UML do CRM Atlas — Visão Clara e Humanizada

Este documento resume a arquitetura e o domínio do **CRM Atlas** usando UML. O objetivo é que qualquer pessoa consiga entender o sistema sem precisar mergulhar no código.

---

## 1. O que é o CRM Atlas?

O CRM Atlas é um sistema de gestão de engenharia. Ele conecta três mundos:

- **Operacional**: acompanhamento de serviços (AVCB, CLCB, processos administrativos e obras), histórico de mudanças e pendências.
- **Financeiro**: orçamentos, contratos, parcelas, recebimentos, custos diretos/indiretos e pagamento de prestadores.
- **Pessoas**: clientes, prestadores e usuários.

---

## 2. Atores

```plantuml
@startuml
left to right direction
actor "Gestor" as Gestor
actor "Operador" as Operador
actor "Financeiro" as Financeiro
actor "Administrador" as Admin
actor "Cliente" as Cliente #LightGrey

rectangle CRM_Atlas {
  usecase "Gerenciar serviços" as UC1
  usecase "Acompanhar situação" as UC2
  usecase "Gerenciar orçamentos" as UC3
  usecase "Converter orçamento em serviço" as UC4
  usecase "Controlar receitas" as UC5
  usecase "Controlar despesas" as UC6
  usecase "Pagar prestadores" as UC7
  usecase "Gerar relatórios e painéis" as UC8
  usecase "Cadastrar clientes e prestadores" as UC9
}

Gestor --> UC8
Gestor --> UC5
Gestor --> UC6
Operador --> UC1
Operador --> UC2
Operador --> UC3
Operador --> UC4
Financeiro --> UC5
Financeiro --> UC6
Financeiro --> UC7
Admin --> UC9
Cliente --> UC2 #LightGrey
@enduml
```

- **Gestor**: vê painéis, acompanha carteira e resultados.
- **Operador**: cadastra orçamentos, converte em serviço, muda situação e observações.
- **Financeiro**: lança entradas, saídas, parcelas e pagamentos.
- **Administrador**: cadastra clientes, prestadores e usuários.
- **Cliente** (passivo): recebe acompanhamento, mas não interage diretamente no sistema.

---

## 3. Diagrama de Classes — Domínio Principal

A classe central do negócio é o `AcompanhamentoServico`. Tudo gira em torno dela: contrato, histórico de situações, pendências e vínculo com prestadores.

```plantuml
@startuml
skinparam classAttributeIconSize 0

class Cliente {
  - Id : long
  - Nome : string
  - Documento : string
  - Telefone : string
  - Email : string
}

class Orcamento {
  - Id : long
  - Codigo : string
  - Nome : string
  - Situacao : string
  - ValorTotal : decimal
}

class CondicaoPagamento {
  - Id : long
  - Nome : string
  - QuantidadeParcelas : int
  - IntervaloDias : int?
}

class CadastroServico {
  - Id : long
  - Codigo : string
  - DataEntrada : DateOnly?
  - SituacaoInicial : string
  - ValorContrato : decimal
  - ValorNotaFiscal : decimal
}

class CadastroServicoParcela {
  - Id : long
  - NumeroParcela : int?
  - Valor : decimal?
  - DataVencimento : DateOnly?
  - FormaPagamento : string
}

class Prestador {
  - Id : long
  - Nome : string
  - CnpjCpf : string
  - MetodoPagamento : string
}

class CadastroServicoPrestador {
  - ValorProvisionado : decimal?
  - ValorEfetivo : decimal?
  - DataPagamentoTipo : string
}

class AcompanhamentoServico {
  - Id : long
  - Codigo : string
  - TipoServico : enum
  - Cliente : string
  - Situacao : string
  - Descricao : string
  - ValorContrato : decimal?
  - DataContrato : DateOnly?
  - AReceber : decimal?
  - Recebido : decimal?
}

class AcompanhamentoServicoHistorico {
  - SituacaoAnterior : string
  - NovaSituacao : string
  - Descricao : string
  - ResponsavelNome : string
  - CreatedAt : DateTime
}

class AcompanhamentoServicoPendencia {
  - Label : string
  - Concluida : bool
  - ConcluidaEm : DateTime?
}

class AcompanhamentoServicoSituacaoConfig {
  - Nome : string
  - Ordem : int?
  - Cor : string
}

class Lancamento {
  - Id : long
  - Codigo : string
  - Descricao : string
  - Valor : decimal?
  - Tipo : string
  - Status : string
  - Data : DateOnly?
}

class CustoIndireto {
  - Id : long
  - Categoria : string
  - Valor : decimal
  - Data : DateOnly
}

class Usuario {
  - Id : long
  - Nome : string
  - Email : string
  - Auth0Id : string
}

' Relacionamentos de associação
Cliente "0..1" --o "0..*" CadastroServico : contratado por
Orcamento "0..1" --o "0..*" CadastroServico : gera
CondicaoPagamento "0..1" --o "0..*" CadastroServico : define parcelas
CadastroServico *-- "0..*" CadastroServicoParcela : possui
CadastroServico *-- "0..*" CadastroServicoPrestador : possui
Prestador "1" -- "0..*" CadastroServicoPrestador : vinculado
CadastroServico "1" -- "1" AcompanhamentoServico : acompanha
AcompanhamentoServico *-- "0..*" AcompanhamentoServicoHistorico : histórico
AcompanhamentoServico *-- "0..*" AcompanhamentoServicoPendencia : pendências
AcompanhamentoServicoSituacaoConfig "1..*" --o "0..*" AcompanhamentoServicoPendencia : define labels
Lancamento "0..*" --o "0..1" CadastroServico : referencia
Lancamento "0..*" --o "0..1" Prestador : paga
@enduml
```

### Principais relações

- **Cliente e CadastroServico**: um cliente pode ter vários serviços; um serviço pertence a um cliente.
- **Orçamento e CadastroServico**: um orçamento, ao ser aprovado, vira um serviço.
- **CadastroServico e AcompanhamentoServico**: são duas faces do mesmo processo — uma é o contrato/financeiro; a outra é o acompanhamento operacional.
- **AcompanhamentoServico, Histórico e Pendências**: toda mudança de situação gera um histórico; cada situação pode ter uma lista de pendências.
- **Lançamento e CadastroServico/Prestador**: os lançamentos financeiros amarram entradas/saídas a serviços e prestadores.

---

## 4. Diagrama de Estados — Serviço

O `AcompanhamentoServico` passa por situações ao longo do tempo. As situações são configuráveis por tipo de serviço.

```plantuml
@startuml
[*] --> EmAnalise : criação / importação
EmAnalise --> EmAndamento : inicia execução
EmAndamento --> AguardandoCliente : falta documento
EmAndamento --> AguardandoDocumentos : falta documento
EmAndamento --> Comunicado : aviso enviado
AguardandoCliente --> EmAndamento : cliente responde
AguardandoDocumentos --> EmAndamento : documentos entregues
Comunicado --> EmAndamento : retomado
EmAndamento --> Concluido : serviço pronto
Concluido --> ConcluidoAguarPag : aguardando pagamento
ConcluidoAguarPag --> Encerrado : pago
Concluido --> Encerrado : pago direto
@enduml
```

- O fluxo não é rígido: o operador pode mudar a situação conforme a realidade.
- Cada mudança grava um **histórico** com a situação anterior, a nova, quem fez e uma observação.

---

## 5. Diagrama de Pacotes — Arquitetura em Camadas

```plantuml
@startuml
package "Frontend" {
  [Blazor Server]
  [MudBlazor Components]
}

package "ApplicationCore" {
  [UseCases]
  [DTOs]
  [Entities]
  [Interfaces]
}

package "Infrastructure" {
  [EF Core / PostgreSQL]
  [Repositories]
  [Migrations]
  [PDF / Excel]
}

package "Integrações" {
  [Auth0]
  [ViaCEP]
  [Google Sheets]
}

[Blazor Server] --> [UseCases]
[UseCases] --> [Entities]
[UseCases] --> [Interfaces]
[Infrastructure] ..> [Interfaces]
[Infrastructure] ..> [Entities]
[Blazor Server] --> [Auth0]
[Infrastructure] --> [ViaCEP]
[Infrastructure] --> [Google Sheets]
@enduml
```

- **Frontend**: Blazor Server com MudBlazor. Não tem regras de negócio; só chama os casos de uso.
- **ApplicationCore**: regras de negócio, entidades e contratos (interfaces).
- **Infrastructure**: acesso a dados, migrations, geração de documentos e integrações externas.
- **Integrações**: Auth0 (login), ViaCEP (endereço) e importação de planilhas.

---

## 6. Dicas para ler os diagramas

- As setas com `*` ou `0..*` significam "muitos". Uma bolinha vazia `o` indica agregação (o filho pode existir sozinho). Um losango cheio `*` indica composição (se o pai morre, o filho também morre, como histórico e pendências de um serviço).
- O `AcompanhamentoServico` é o coração do sistema. Quase todas as consultas e relatórios partem dele.
- `CadastroServico` e `AcompanhamentoServico` compartilham o mesmo `Codigo` na prática, mas um cuida do financeiro/contrato e o outro da operação.

---

## 7. Resumo em uma frase

> O CRM Atlas liga **pessoas** (clientes e prestadores), **contratos** (serviços e orçamentos) e **movimentações** (receitas, despesas e histórico operacional) em uma única base, permitindo acompanhar a fila de trabalho, a carteira a receber e o resultado financeiro.
