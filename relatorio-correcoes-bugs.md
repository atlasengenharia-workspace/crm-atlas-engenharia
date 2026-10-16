# Relatório de Correções — CRM Atlas Engenharia

Bugs reportados pelo cliente × correções aplicadas

---

## Tela Orçamentos (`/orcamentos`)

| # | Reclamação do cliente | Correção aplicada |
|---|---|---|
| 1 | Coluna "Semana" sem filtro e sem ordenação. | ✅ **Resolvido** — Coluna ligada à propriedade `Semanas` do DTO — filtro e ordenação ativos no grid. |
| 2 | Filtro "Situação" sem a opção "aguardando cliente". | ✅ **Resolvido** — Opções passam a unir catálogo configurado + situações em uso + padrões por tipo — "aguardando cliente" e valores legados aparecem. |
| 3 | Semana só contava após 7 dias (enviado sexta "zerava" até a sexta seguinte). | ✅ **Resolvido** — `SemanasCivil` conta viradas de semana civil (segunda→segunda), igual ao Excel: enviado sexta → 1 semana já na segunda seguinte. |

## Tela Serviço (`/acompanhamento` + `/cadastros-servico`)

| # | Reclamação do cliente | Correção aplicada |
|---|---|---|
| 4 | Campo "Subtipo" sem opções / não editável. | ✅ **Resolvido** — Campo editável com catálogo de subtipos; valores legados preservados nas opções. |
| 5 | Campo "Situação" sem opções no cadastro inicial. | ✅ **Resolvido** — Formulário e tabela usam o mesmo catálogo: configurado + em uso + padrões (`AcompanhamentoSituacaoDefaults`) por tipo. |
| 6 | Alterar o código gerava serviço novo em vez de atualizar o original. | ✅ **Resolvido** — Sincronização busca pelo código antigo, renomeia o acompanhamento, propaga o novo código aos lançamentos e registra no histórico. |
| 7 | Código não era gerado automaticamente no formato da empresa. | ✅ **Resolvido** — Geração segue as séries reais: AVCB sequencial (3101), CLCB 4xxx, OBRAS N.NN (2.85), PROCESSOS_ADM 5xxx. |
| 8 | Busca CNPJ falhava; "razão social" ficava bloqueada (texto qualquer ou vínculo com orçamento). | ✅ **Resolvido** — Consulta só dispara com exatamente 14 dígitos; lookup preenche apenas campos vazios; formulário vindo de orçamento permanece editável. |
| 9 | Faltava a coluna "Nome do contato". | ✅ **Resolvido** — Contato incluído no DTO, na grade, no filtro, na busca global, no PDF e no Excel. |
| 10 | Não era possível remover pendências individuais de um serviço. | ✅ **Resolvido** — Botão de exclusão por pendência + `RemovePendingAsync` no serviço. |
| 11 | "A receber" calculado errado nos serviços criados pelo site (importados ok). | ✅ **Resolvido** — `Recebido`/`Custos`/`AReceber` somam os lançamentos pagos criados no site aos valores armazenados. |
| 12 | "Situação" da tabela diferente do campo dentro do cadastro. | ✅ **Resolvido** — Ambos consomem a mesma união de catálogos (ver item 5). |
| 13 | Faltava botão de pasta na coluna "Ação". | ✅ **Resolvido** — Botão de pasta (Drive/local) adicionado à coluna de ações. |
| 14 | "Copiar endereço" anexava o código do serviço no final e quebrava o caminho. | ✅ **Resolvido** — Caminho absoluto/UNC é copiado intacto; o código só é anexado a caminho relativo com raiz configurada. |
| 15 | Coluna oculta (ex.: "Pendências") voltava a aparecer sozinha. | ✅ **Resolvido** — `SmartTableFilter` notifica as colunas ocultas após restaurar a preferência salva no navegador. |

## Tela Lançamentos (`/financeiro/lancamentos`)

| # | Reclamação do cliente | Correção aplicada |
|---|---|---|
| 16 | Selecionar parcela trocava a forma de pagamento para "3x"/"4x". | ✅ **Resolvido** — Parcela nunca mais sobrescreve a forma de pagamento (só preenche se vazio e rejeita rótulos de parcelamento). |
| 17 | Lançamentos criados pelo site ficavam como "Previsto"; deveriam ser todos pagos. | ✅ **Resolvido** — Padrão do formulário passou a `PAGO`; SQL de inicialização corrige os registros existentes criados manualmente. |
| 18 | Sem aviso de repetição (mesmo valor e data). | ✅ **Resolvido** — Mesmo valor + mesma data + mesmo tipo exige confirmação antes de salvar. |
| 19 | Sem aviso de sobrevalor. | ✅ **Resolvido** — Valor acima da parcela (ou do contrato) exige confirmação antes de salvar. |
| 20 | Erro "saving entity changes" ao criar lançamento. | ✅ **Resolvido** — Geração de código passou de `count+1` (colidia após exclusões) para `max+1` com retry em violação de unicidade. |

## Geral

| # | Reclamação do cliente | Correção aplicada |
|---|---|---|
| 21 | Formulário preenchido era apagado ao voltar na tela ou quando a página atualizava em tempo real. | ✅ **Resolvido** — `FormDraftStore` preserva o rascunho ao sair e restaura ao voltar; a tela não é recriada com formulário aberto. Cobre orçamento, serviço, lançamento, acompanhamento, clientes, prestadores, custos, notificações e importações. |
| 22 | Busca global não achava por texto parcial. | ✅ **Resolvido** — Busca sem limite de 100 registros por tipo, inclui concluídos e texto de contato. |

---

## Verificação de qualidade

- **22/22** reclamações resolvidas.
- Build limpo: **0 erros, 0 avisos**.
- Testes: **33/33** unitários e **16/16** de integração aprovados.
- 20 arquivos alterados + 4 novos (`SemanasCivil`, `SituacaoFinalizada`, `AcompanhamentoSituacaoDefaults`, `FormDraftStore`).
- Consistência conferida em todas as telas afetadas: `/orcamentos`, `/acompanhamento`, `/cadastros-servico`, `/financeiro/lancamentos`, `/busca` e relatórios PDF/Excel.

---

*CRM Atlas Engenharia — Relatório de correções — Gerado em 16/10/2026*
