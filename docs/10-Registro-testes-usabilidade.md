# 10 — Registro de testes de usabilidade da API

- **Versão:** 2.1
- **Data da última mudança:** 05/11/2026
- **Substitui:** versão 1.0 (30/10/2026)

As tarefas, critérios e roteiro estão no [plano de testes de usabilidade](09-Plano-testes-usabilidade.md).

---

## 0. Verificações de contrato já feitas

Estas verificações são automáticas e não contam como rodada de usabilidade.

| Verificação | Resultado | Data |
| ----------- | --------- | ---- |
| `docs/openapi.yaml` validado como OpenAPI 3.0.3 | Não executado: arquivo não está presente nesta cópia local | 05/11/2026 |
| OpenAPI gerado comparado com o código atual | `POST /api/triagem` contém somente `relato` | 05/11/2026 |
| Contrato atual contra a resposta real da API | Oito casos HTTP conformes; contrato válido retorna 501 enquanto UC01 está pendente | 05/11/2026 |

---

## 1. Desempenho de ponta a ponta (RNF04)

Mede o tempo entre o envio do pedido e a resposta completa, em três execuções por caso. A meta é 15
segundos ou menos, a validar após a primeira rodada, porque depende da chamada ao Gemini. Registre o
método de medição usado (`Measure-Command` ou `curl.exe -w "%{time_total}"`).

| Caso | Tipo | Execução 1 (s) | Execução 2 (s) | Execução 3 (s) | Média (s) | Dentro de 15 s |
| ---- | ---- | -------------- | -------------- | -------------- | --------- | -------------- |
| CT-01 | Pedido claro, ficha gravada | a medir | a medir | a medir | a medir | a medir |
| CT-09 | Pedido com vários sintomas | a medir | a medir | a medir | a medir | a medir |
| CT-13 | Pedido com pergunta de esclarecimento (422) | a medir | a medir | a medir | a medir | a medir |
| CT-20 | Pedido com linguagem informal | a medir | a medir | a medir | a medir | a medir |

**Resumo:** média geral e máximo registrado: a medir.

---

## 2. Tarefas de integração

Uma linha por tarefa e por participante. Use "concluída", "com ajuda" ou "não concluída".

| Tarefa | Participante | Resultado | Tempo (s) | Observação |
| ------ | ------------ | --------- | --------- | ---------- |
| T1 | a registrar | a medir | a medir | a medir |
| T2 | a registrar | a medir | a medir | a medir |
| T3 | a registrar | a medir | a medir | a medir |
| T4 | a registrar | a medir | a medir | a medir |
| T5 | a registrar | a medir | a medir | a medir |
| T6 | a registrar | a medir | a medir | a medir |

---

## 3. Divergências entre contrato e resposta real

Registre cada diferença encontrada entre o `openapi.yaml` e a resposta da API, com data e endpoint.
Uma divergência é corrigida no contrato ou no código, nunca apenas anotada.

| Data | Endpoint | Esperado (`openapi.yaml`) | Obtido | Correção | Resolvido |
| ---- | -------- | ------------------------- | ------ | -------- | --------- |
| a registrar | a registrar | a registrar | a registrar | a registrar | a registrar |

Se não houver divergência durante a rodada, escreva "nenhuma divergência registrada".

---

## 4. Fricções de integração (feedback)

Registre cada ponto que o participante marcou como confuso, mesmo que a tarefa tenha sido concluída.

| Data | Participante | Ponto de fricção | Sugestão do participante | Decisão |
| ---- | ------------ | ---------------- | ------------------------ | ------- |
| a registrar | a registrar | a registrar | a registrar | a registrar: aceito, recusado ou adiado (com motivo) |

---

## 5. Travamentos e erros de interface

Registre qualquer erro 500, resposta sem ProblemDetails, tempo de resposta que expirou ou resposta
vazia, com data, hora e caso.

| Data e hora | Endpoint | Caso ou tarefa | Descrição | Resolvido |
| ----------- | -------- | -------------- | --------- | --------- |
| a registrar | a registrar | a registrar | a registrar | a registrar |

Se não houver nenhum erro durante a rodada, escreva "nenhum erro registrado" nesta seção.

---

## 6. Conclusão

- **Tempo dentro da meta:** a registrar.
- **Tarefas concluídas sem ajuda:** a registrar.
- **Divergências de contrato encontradas e corrigidas:** a registrar.
- **Problemas de integração encontrados:** a registrar.
- **Mudança proposta para o contrato ou a documentação:** a registrar, ou "nenhuma" se o resultado for satisfatório.

---

## 7. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.1 | 05/11/2026 | Corrige o registro: `docs/openapi.yaml` está ausente e registra a verificação real do OpenAPI gerado e do contrato HTTP parcial. |
| 2.0 | 04/11/2026 | Cria o registro de usabilidade da API. |
