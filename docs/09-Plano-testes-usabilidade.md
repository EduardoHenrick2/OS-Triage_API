# 09 — Plano de testes de usabilidade da API

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (30/10/2026), que previa usabilidade de uma interface visual

---

## 1. Objetivo

A versão 2.0 não tem interface visual. A usabilidade é a dos contratos REST: quem integra a API
deve conseguir descobri-la, chamá-la e tratar as respostas sem apoio do desenvolvedor do projeto.
O teste verifica quatro coisas:

1. A pessoa encontra e chama `POST /api/triagem` a partir da documentação OpenAPI.
2. A pessoa interpreta as respostas de erro (400, 422, 503) e sabe a ação seguinte.
3. A pessoa usa `Idempotency-Key` corretamente e não cria ordens duplicadas.
4. A documentação e as respostas reais coincidem, sem divergência de campos ou de códigos.

Os resultados alimentam o [registro de testes de usabilidade](10-Registro-testes-usabilidade.md).

---

## 2. Participantes e papéis

Os perfis vêm da [especificação](02-Especificacao.md), seção 2, e são de dois tipos:

- **Integrador:** desenvolvedor que vai conectar um sistema ou um script à API. É o perfil principal deste teste.
- **Atendimento:** quem envia relatos pelo cliente REST ou pelo canal de chat. Participa das tarefas de envio e de leitura da ficha.

Nesta versão, as tarefas são executadas pelo desenvolvedor do projeto, que segue o roteiro sem orientar
a leitura da documentação. Um integrador externo é desejável. Se houver disponibilidade, ele executa
as mesmas tarefas, e o resultado é registrado no documento 10.

---

## 3. Superfície de teste

- **Documentação:** `docs/openapi.yaml`, lido diretamente ou pela interface Swagger UI. A Swagger UI deve estar disponível em ambiente de desenvolvimento. Confirmar antes da rodada.
- **Cliente HTTP:** `curl.exe` ou `Invoke-RestMethod`, como nos exemplos do [manual de instalação](12-Manual-instalacao.md).
- **Relatos de teste:** somente dados fictícios, com os casos do gabarito indicados em cada tarefa.

Os canais de chat ficam fora deste teste de usabilidade. O webhook do Telegram exige endereço HTTPS
público e é coberto por testes de integração.

---

## 4. Tarefas

| ID | Tarefa | Ponto de partida | Conclusão esperada |
| -- | ------ | ---------------- | ------------------ |
| T1 | Enviar um relato completo e obter a ficha | Abrir a documentação do `POST /api/triagem` e chamar com o caso CT-01 | Resposta 200 com ficha, urgência e mensagem ao cliente |
| T2 | Enviar um relato sem o modelo da moto e responder à pergunta | Chamar com o caso CT-13 | Resposta 422 lida corretamente; nova chamada com o modelo gera 200, sem reler a documentação |
| T3 | Provocar um 400 e corrigir o pedido | Enviar relato vazio, depois um campo desconhecido no corpo | Erro compreendido a partir do ProblemDetails; pedido corrigido na segunda tentativa |
| T4 | Reenviar o mesmo pedido com a mesma chave de idempotência | Chamar duas vezes com o mesmo `Idempotency-Key` | Lista de ordens com uma única ordem para aquele relato |
| T5 | Localizar uma ordem gravada | `GET /api/ordens-servico` com filtro de urgência e, depois, `GET /api/ordens-servico/{id}` | Ordem da T1 encontrada em até duas chamadas |
| T6 | Agir diante de um 503 | Parar o contêiner do PostgreSQL e enviar um relato (TE06) | Explica que deve esperar, respeitar `Retry-After` e reenviar com a mesma chave, sem criar duplicata |

---

## 5. Critérios de aceitação

| Critério | Medida | Meta |
| -------- | ------ | ---- |
| Conclusão sem ajuda | Tarefas concluídas sem pedir orientação, sobre o total | T1 a T6 todas concluídas |
| Interpretação de erro | Ação seguinte descrita corretamente para 400, 422 e 503 | Três de três na primeira leitura |
| Uso de idempotência | Ordens duplicadas criadas pelo participante | Zero |
| Tempo até a primeira chamada bem-sucedida | Minutos desde a abertura da documentação | A validar após a primeira rodada |
| Divergência entre contrato e resposta | Campos, tipos ou códigos da resposta real diferentes do `openapi.yaml` | Zero (registrada no documento 10) |
| Fricção de integração | Pontos que o participante marcou como confusos (nome de campo, tipo, exemplo faltando) | Registrados, com decisão para cada um |

---

## 6. Roteiro de observação

Para cada tarefa, anote:

- Por onde a pessoa começou: a documentação, um exemplo, ou o código-fonte.
- Se abriu o `openapi.yaml` e em qual seção parou.
- Qual status HTTP causou dúvida e qual frase da resposta foi lida em voz alta ou comentada.
- Quantas tentativas foram feitas antes de acertar.
- Se a pessoa precisou perguntar algo ao desenvolvedor do projeto. Cada pergunta é um ponto de fricção.

---

## 7. Checklist de usabilidade de API

Além das tarefas, avalie o contrato com estes critérios, adaptados das heurísticas de Nielsen. Marque
"ok" ou "problema", com o endpoint afetado.

- [ ] Todo endpoint aparece no `openapi.yaml` com exemplo de requisição e de resposta.
- [ ] Nomes de campos seguem `camelCase` em todas as respostas e exemplos.
- [ ] Erros 400 e 422 dizem qual campo falhou e por quê.
- [ ] Respostas 503 e 429 trazem `Retry-After` e o código de falha.
- [ ] A documentação explica que `Idempotency-Key` evita duplicata e como gerar a chave.
- [ ] Os exemplos usam dados fictícios e não mostram nenhum segredo.
- [ ] Os códigos de status são previsíveis: o mesmo tipo de erro sempre gera o mesmo status.
- [ ] A lista de ordens tem filtro e paginação descritos no contrato.

---

## 8. Limites deste teste

- Um ou dois participantes não representam todos os integradores. O resultado serve para encontrar
  problemas de contrato, não para medir satisfação.
- O teste não avalia a qualidade da extração do LLM. Essa avaliação está no [plano de testes](07-Plano-testes-software.md).
- Divergências entre contrato e resposta real também são cobertas por testes de contrato automatizados.
  Este teste complementa esses testes com a leitura humana da documentação.
