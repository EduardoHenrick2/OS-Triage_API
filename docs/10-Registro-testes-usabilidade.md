# 10 — Registro de testes de usabilidade da API

- **Versão:** 2.3
- **Data da última mudança:** 07/11/2026
- **Substitui:** versão 2.2 (05/11/2026)

As tarefas e critérios estão no plano de testes de usabilidade, documento 09.

---

## 1. Evidências automáticas disponíveis

Estas verificações apoiam a rodada, mas não contam como teste de usabilidade com participante.

| Verificação | Resultado | Data |
| ----------- | --------- | ---- |
| Build de `OsTriageApi` | 0 erros e 0 avisos | 07/11/2026 |
| Suíte `OsTriageApi.Tests.Unit` | 59 aprovados, 0 falhas, 0 ignorados | 07/11/2026 |
| Contrato de entrada | Somente `relato`; campos desconhecidos são rejeitados | 07/11/2026 |
| UC01 com dependências simuladas | 200, 422, 502, 503, idempotência, risco e ano cobertos | 07/11/2026 |
| Docker Compose | Configuração válida; imagem não construída porque o daemon Docker não estava disponível | 07/11/2026 |
| OpenAPI versionado | Pendente: `docs/openapi.yaml` não existe nesta cópia | 07/11/2026 |

A verificação anterior de oito respostas HTTP com 501 pertence à baseline anterior ao UC01 e não
comprova o comportamento atual.

---

## 2. Rodada com participante

Ainda não executada. Não há tempos nem resultados de usabilidade a registrar.

| Tarefa | Participante | Resultado | Tempo | Observação |
| ------ | ------------ | --------- | ----- | ---------- |
| T1 — CT-01 | a registrar | não executada | — | Depende de PostgreSQL e Gemini |
| T2 — CT-13 | a registrar | não executada | — | Depende de PostgreSQL e Gemini |
| T3 — contrato inválido | a registrar | não executada | — | — |
| T4 — idempotência | a registrar | não executada | — | — |
| T5 — saída inválida | a registrar | não executada | — | — |
| T6 — indisponibilidade | a registrar | não executada | — | — |

---

## 3. Desempenho de ponta a ponta

| Caso | Execução 1 | Execução 2 | Execução 3 | Média | Meta de 15 s |
| ---- | ---------- | ---------- | ---------- | ----- | ------------ |
| CT-01 | a medir | a medir | a medir | a medir | a avaliar |
| CT-13 | a medir | a medir | a medir | a medir | a avaliar |

A medição depende de chamada real ao Gemini e não foi estimada.

---

## 4. Divergências entre contrato e resposta

| Data | Endpoint | Esperado | Obtido | Correção | Situação |
| ---- | -------- | -------- | ------ | -------- | -------- |
| a registrar | `POST /api/triagem` | Documento 04 e OpenAPI gerado | a registrar | a registrar | rodada pendente |

---

## 5. Fricções, travamentos e erros

| Data | Tarefa | Tipo | Descrição | Decisão |
| ---- | ------ | ---- | --------- | ------- |
| a registrar | a registrar | fricção, travamento ou erro | a registrar | a registrar |

---

## 6. Conclusão

- **Rodada de usabilidade:** não executada.
- **Integração automatizada:** 59 testes unitários aprovados.
- **Próximo passo:** iniciar PostgreSQL e API, configurar credenciais e executar T1 a T6.

---

## 7. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.3 | 07/11/2026 | Atualiza as evidências para o UC01, 59 testes xUnit e Compose com API; mantém a rodada humana como não executada. |
| 2.2 | 05/11/2026 | Marca o teste 501 como baseline anterior e registra dependências externas. |
| 2.1 | 05/11/2026 | Registra o OpenAPI gerado e a ausência do arquivo versionado. |
| 2.0 | 04/11/2026 | Cria o registro de usabilidade da API. |
