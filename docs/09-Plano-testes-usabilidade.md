# 09 — Plano de testes de usabilidade da API

- **Versão:** 2.1
- **Data da última mudança:** 07/11/2026
- **Substitui:** versão 2.0 (04/11/2026)

---

## 1. Objetivo

Avaliar se uma pessoa integradora consegue usar o contrato REST de `POST /api/triagem` sem ajuda
do autor. O teste verifica descoberta do contrato, montagem da requisição, interpretação de
ProblemDetails, tratamento de 422/502/503 e uso de `Idempotency-Key`.

A qualidade da classificação do Gemini é medida pelo plano de testes de software. Este documento
avalia a experiência de integração com a API.

---

## 2. Participantes e papéis

| Papel | Responsabilidade no teste |
| ----- | ------------------------- |
| Integrador | Ler o contrato, executar as chamadas e explicar a ação seguinte |
| Atendimento | Conferir se ficha, pergunta e aviso de urgência são compreensíveis |
| Observador | Registrar tempo, dúvidas, erros e intervenções necessárias |

Todos os relatos usados na sessão são fictícios.

---

## 3. Ambiente e materiais

- API e PostgreSQL iniciados por Docker Compose, com portas publicadas somente em `127.0.0.1`.
- Contrato OpenAPI gerado pela API em `/openapi/v1.json` no ambiente Development.
- Documento 04 como referência dos exemplos e dos formatos de resposta.
- Cliente HTTP `curl.exe`, `Invoke-RestMethod`, Postman ou equivalente.
- Evidência automatizada em `OsTriageApi.Tests.Unit`; os testes xUnit não substituem a sessão com o participante.
- Segredos fornecidos por user-secrets na execução local ou por Docker secrets no Compose.

O webhook do Telegram, consultas de ordens e interface visual permanecem fora desta rodada.

---

## 4. Tarefas

| ID | Tarefa | Ponto de partida | Conclusão esperada |
| -- | ------ | ---------------- | ------------------ |
| T1 | Enviar CT-01 | Ler o contrato de `POST /api/triagem` | 200 com ficha, urgência e mensagem |
| T2 | Enviar CT-13 | Usar o mesmo contrato | 422 com `modeloMoto` em `camposFaltando` e pergunta clara |
| T3 | Corrigir contrato inválido | Enviar relato vazio e depois campo desconhecido | 400 entendido e requisição corrigida |
| T4 | Repetir uma chave | Enviar duas vezes com o mesmo `Idempotency-Key` | Mesmo `idOrdem`, sem nova extração nem duplicata |
| T5 | Interpretar falha do modelo | Simular saída inválida três vezes | 502 com `saida_llm_invalida` entendido |
| T6 | Interpretar indisponibilidade | Simular Gemini ou PostgreSQL indisponível | 503, `Retry-After` e código de falha entendidos |

---

## 5. Critérios de aceitação

| Critério | Meta |
| -------- | ---- |
| Tarefas concluídas sem intervenção | 6 de 6 |
| Ação seguinte correta após 400, 422, 502 e 503 | 4 de 4 |
| Duplicatas com a mesma chave | Zero |
| Divergências entre documento 04, OpenAPI gerado e resposta real | Zero |
| Segredos expostos em comando, resposta ou captura | Zero |
| Pontos de fricção sem decisão registrada | Zero |

---

## 6. Roteiro de observação

Para cada tarefa, registre:

- material consultado primeiro;
- comando ou cliente utilizado;
- status HTTP obtido;
- interpretação verbal da resposta;
- quantidade de tentativas;
- ajuda solicitada;
- divergência entre documentação e comportamento;
- sugestão do participante.

---

## 7. Checklist do contrato REST

- [ ] `POST /api/triagem` aparece no OpenAPI gerado com apenas o campo `relato`.
- [ ] Campos desconhecidos geram 400 com erro na chave `body`.
- [ ] Campos obrigatórios ausentes aparecem em `camposFaltando`.
- [ ] 502 e 503 usam ProblemDetails e códigos estáveis.
- [ ] 503 inclui `Retry-After`.
- [ ] O documento explica a finalidade de `Idempotency-Key`.
- [ ] Exemplos usam dados fictícios e não contêm segredos.
- [ ] Categoria e urgência usam os valores canônicos documentados.

---

## 8. Limites

- Uma sessão com poucos participantes encontra problemas, mas não mede satisfação geral.
- A chamada real depende de PostgreSQL, credenciais e cota do Gemini.
- O plano não cobre carga, Telegram, consultas ou avaliação completa dos 24 casos.

---

## 9. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.1 | 07/11/2026 | Restringe a rodada ao contrato REST implementado e integra evidências xUnit, ProblemDetails, idempotência e Docker Compose. |
| 2.0 | 04/11/2026 | Primeira versão voltada à usabilidade da API. |
