# 08 — Registro de testes de software

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (30/10/2026)

Este registro reúne três tipos de evidência: execuções das suítes xUnit, integridade do gabarito
e rodadas de avaliação do prompt. As metas são as da seção 6 do [plano de testes](07-Plano-testes-software.md).

---

## 1. Execuções das suítes de teste

Uma linha por execução. Preencha a data e o total reais da rodada. Não estime valores.

| Data | Suíte | Escopo | Ambiente | Total | Passou | Falhou | Ignorado | Situação |
| ---- | ----- | ------ | -------- | ----- | ------ | ------ | -------- | -------- |
| 04/11/2026 | Compilação de `src/OsTriageApi` | Projeto principal | SDK .NET 10.0.114, ambiente de desenvolvimento em nuvem | — | — | — | — | Concluída: 0 erros, 0 avisos, com avisos tratados como erro |
| 04/11/2026 | Unidade (`tests/OsTriageApi.Tests`, xUnit) | Normalização, risco, contrato, saída do LLM, decisor, categorias | SDK .NET 10.0.114, ambiente de desenvolvimento em nuvem | a executar | a executar | a executar | a executar | Escrita, mas não executada: o NuGet não está acessível neste ambiente (bloqueio de política do proxy). Executar com `dotnet test` na máquina do desenvolvedor e registrar a linha aqui |
| 04/11/2026 | Verificação de núcleo (`tests/Verificacao`, sem pacotes externos) | Mesmos grupos de caso da suíte de unidade, em programa de console | SDK .NET 10.0.114, ambiente de desenvolvimento em nuvem | 44 | 44 | 0 | 0 | Checagem de ambiente, não é suíte oficial. Substituída pela suíte xUnit e removida antes do commit |
| a registrar | Integração (`tests/OsTriageApi.Tests.Integracao`) | Endpoints, EF Core, idempotência, PostgreSQL em Testcontainers | a registrar | a executar | a executar | a executar | a executar | Ainda não criada (fase 3) |
| a registrar | Avaliação (`tests/OsTriageApi.Tests.Avaliacao`) | 24 casos do gabarito, com 3 execuções cada | a registrar | a executar | a executar | a executar | a executar | Ainda não criada (fase 4). Executada somente com `AVALIACAO_HABILITADA=true` |

**Regra de registro:** a linha só recebe números de uma execução real. Quando uma suíte não roda, a
célula de situação diz o motivo.

---

## 2. Integridade do gabarito

O gabarito de 24 casos fica congelado. Qualquer alteração exige registro com data e motivo, e uma
nova rodada de medição.

| Verificação | Resultado | Data |
| ----------- | --------- | ---- |
| Hash SHA-256 das 24 linhas da seção 5.2 do [plano de testes](07-Plano-testes-software.md), sem quebras de linha de Windows (CR) | `1c8858d34255eaa57684af6b6b916d1f16850f1b970f1ab64bc81faedcd3521b` | 04/11/2026 |
| Hash igual ao do gabarito recebido no pacote original de planejamento | Sim | 04/11/2026 |
| Congelamento no commit `dcd385f` | Pendente: o desenvolvedor valida o commit localmente antes do primeiro push | a registrar |

A seção 5.4 do plano de testes tem uma tabela de comparação com colunas parecidas com as do
gabarito. Ela não faz parte do gabarito, por isso não entra no hash.

---

## 3. Lotes de execução do gabarito (fase 4)

Uma linha por lote. Um lote é a rodada completa dos 24 casos, com 3 execuções cada, que gera 72 registros de execução.

| Data | Versão do prompt | Modelo (valor de `Gemini:Modelo`) | Temperatura | Commit | Execuções | Erros de API | Tempo médio (s) | Tempo máximo (s) | Observações |
| ---- | ---------------- | --------------------------------- | ----------- | ------ | --------- | ------------ | --------------- | ---------------- | ----------- |
| a registrar | v1 | a registrar | a registrar | a registrar | a medir | a medir | a medir | a medir | a medir |
| a registrar | v2 | a registrar | a registrar | a registrar | a medir | a medir | a medir | a medir | a medir |

---

## 4. Métricas por versão do prompt

As metas são as da seção 6 do plano de testes. A coluna "Atingiu (v2)" recebe "sim" ou "não" na fase 4.

| Métrica | Meta v1 | v1 | v2 | Atingiu (v2) |
| ------- | ------- | -- | -- | ------------ |
| Validade do JSON | 100%, com até 2 novas tentativas | a medir | a medir | a medir |
| Acerto de categoria | 85% ou mais | a medir | a medir | a medir |
| Acerto de urgência | 80% ou mais | a medir | a medir | a medir |
| Urgência alta não perdida | 100% | a medir | a medir | a medir |
| Falsos alertas | 10% ou menos (a validar) | a medir | a medir | a medir |
| Ganho da regra de segurança | Informativo | a medir | a medir | não se aplica |
| Extração de modelo e ano | 90% ou mais | a medir | a medir | a medir |
| Decisão de perguntar | 100% | a medir | a medir | a medir |
| Invenção de dados | 0 | a medir | a medir | a medir |
| Consistência | 90% ou mais | a medir | a medir | a medir |
| Tempo por pedido | Até 15 s (a validar) | a medir | a medir | a medir |

---

## 5. Códigos de falha da saída do LLM

Cada rejeição de `ValidadorSaidaLlm` grava um código em `codigo_erro`. A contagem é por ocorrência,
nas 3 execuções de cada caso. A coluna "Tipo de erro" liga o código ao agrupamento do ciclo de
ajuste (seção 7 do plano de testes).

| Código (`codigo_erro`) | Tipo de erro | v1 | v2 | Casos afetados (IDs) |
| ---------------------- | ------------ | -- | -- | -------------------- |
| `json_invalido` | Formato | a medir | a medir | a medir |
| `nao_e_objeto` | Formato | a medir | a medir | a medir |
| `campo_ausente:<campo>` | Campo inventado ou ausente | a medir | a medir | a medir |
| `tipo_invalido:<campo>` | Formato | a medir | a medir | a medir |
| `categoria_invalida` | Categoria | a medir | a medir | a medir |
| `urgencia_invalida` | Urgência | a medir | a medir | a medir |
| `ano_invalido` | Campo inventado | a medir | a medir | a medir |

---

## 6. Testes especiais

Os IDs e o comportamento esperado vêm da seção 4.3 do [plano de testes](07-Plano-testes-software.md).

| ID | Teste | v1 | v2 | Observações |
| -- | ----- | -- | -- | ----------- |
| TE01 | Chave do Gemini inválida (RF09) | a medir | a medir | Esperado: 503 com `ia_indisponivel` e `Retry-After`, sem repetição |
| TE02 | Resposta fora do esquema (RNF03) | a medir | a medir | Esperado: até duas novas tentativas; 502 se persistir |
| TE03 | Categoria fora da lista (RF03) | a medir | a medir | Esperado: rejeitada em código; nova tentativa |
| TE04 | Falha após a gravação e repetição do envio (RF06) | a medir | a medir | Esperado: uma única ordem; a segunda resposta devolve a ficha original |
| TE05 | Limite de cota (RF09) | a medir | a medir | Esperado: sem repetição; 503 imediata com `Retry-After` |
| TE06 | Banco indisponível | a medir | a medir | Esperado: 503 com `banco_indisponivel`; nenhuma resposta de sucesso |

---

## 7. Mudança entre v1 e v2

Uma frase por mudança, na forma "categoria: de X% na v1 para Y% na v2". Inclua a mudança feita no
prompt, que deve ser uma só, e os erros que continuam depois da v2.

| Métrica | De (v1) | Para (v2) | Mudança feita no prompt |
| ------- | ------- | --------- | ----------------------- |
| a medir | a medir | a medir | a medir |

---

## 8. Conclusão da rodada

- **Versão mantida:** a registrar.
- **Metas atingidas:** a registrar.
- **Erros restantes e causa provável:** a registrar.
- **Próxima ação:** a registrar, ou "encerrar a fase 4" se as metas foram atingidas.
