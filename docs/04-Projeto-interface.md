# 04 — Projeto de interface e contratos de API

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0, que descrevia o formulário visual do n8n (Form Trigger)
- **Contrato de referência:** [openapi.yaml](openapi.yaml) (OpenAPI 3.0.3)

---

## 1. Visão geral

A versão 1 não tem interface visual própria. A interação acontece por dois canais, ambos
com contrato HTTP:

| Canal | Cliente | Endpoint |
| ----- | ------- | -------- |
| Atendimento (REST) | Aplicação de balcão em evolução, ferramenta de teste ou cURL | `POST /api/triagem` |
| Atendimento (Telegram) | Bot do Telegram, que chama a API a cada mensagem | `POST /api/telegram/webhook` |
| Consulta de ordens | Ferramenta do mecânico, planilha que aceite JSON, ou cURL | `GET /api/ordens-servico` e `GET /api/ordens-servico/{id}` |

A API é um serviço ASP.NET Core. Em desenvolvimento, o documento OpenAPI gerado pelo próprio
runtime fica em `/openapi/v1.json`. A interface Swagger UI pode ser ligada na mesma
configuração. O arquivo `docs/openapi.yaml` é a referência versionada, e um teste
automatizado garante que ele coincide com o que a API expõe (HU16).

**Endereço base de desenvolvimento:** `http://localhost:5000` (configurável). A API escuta
somente em `127.0.0.1` (RNF10).

---

## 2. Convenções

| Tema | Regra |
| ---- | ----- |
| Formato | JSON em UTF-8. Cabeçalho `Content-Type: application/json` em todos os `POST` |
| Nomes de campos | `camelCase`, padrão do ASP.NET Core. Exceção: o corpo do webhook do Telegram segue o formato da Telegram Bot API (`snake_case`) |
| Identificadores | UUID (`idPedido`, `idOrdem`) |
| Datas | ISO 8601 em UTC com sufixo `Z`, por exemplo `2026-11-04T14:05:00Z`. A conversão para `America/Sao_Paulo` é feita pelo cliente |
| Urgência | `baixa`, `media`, `alta`. Minúsculas e sem acento |
| Categoria | Uma das oito formas canônicas, com acento quando houver (seção 4.1 de [06-Arquitetura](06-Arquitetura-solucao.md)) |
| Status da ordem | `aberta`, `em_execucao`, `concluida`, `cancelada`. Na v1, toda ordem nasce `aberta` |
| Erros | RFC 9457 (`application/problem+json`), com as extensões descritas na seção 6 |
| Rastreio | Cabeçalho `X-Request-Id` ecoado na resposta, quando enviado, ou gerado pela API. Também aparece em `traceId` no erro |
| Versão | Sem prefixo de versão na URL na v1. Mudança que quebra o contrato gera nova versão da rota (`/api/v2/...`) |

### 2.1 Correspondência entre os nomes do domínio e os campos da API

Os nomes da especificação (`modelo_moto`, `urgencia_final`) viram `camelCase` na API e
`snake_case` no banco.

| Domínio (spec) | API (JSON) | Banco (coluna) |
| -------------- | ---------- | -------------- |
| `modelo_moto` | `modeloMoto` | `modelo_moto` |
| `urgencia_llm` | `urgenciaLlm` | `urgencia_llm` |
| `urgencia_final` | `urgenciaFinal` | `urgencia_final` |
| `pecas_provaveis` | `pecasProvaveis` | `pecas_provaveis` |
| `campos_faltando` | `camposFaltando` | não persistido (só na resposta) |
| `pergunta_ao_atendente` | `perguntaAoAtendente` | não persistido (só na resposta) |
| `campos_nao_confirmados` | `camposNaoConfirmados` | `campos_nao_confirmados` |

---

## 3. Resumo dos endpoints

| Método | Rota | Função | Códigos de sucesso | Códigos de erro |
| ------ | ---- | ------ | ------------------ | --------------- |
| `POST` | `/api/triagem` | Registra um relato (UC01) | 200, 422 | 400, 415, 429, 502, 503, 500 |
| `POST` | `/api/telegram/webhook` | Recebe uma mensagem do bot (UC01, RF14) | 200 | 400, 401, 500 |
| `GET` | `/api/ordens-servico` | Lista ordens com filtros (UC02) | 200 | 400, 500 |
| `GET` | `/api/ordens-servico/{id}` | Mostra uma ordem (UC02) | 200 | 400, 404, 500 |

---

## 4. `POST /api/triagem`

Registra um relato de cliente e devolve a ficha, a pergunta ou o erro. É o contrato do
atendimento pelo cliente REST.

### 4.1 Requisição

**Cabeçalhos**

| Cabeçalho | Obrigatório | Descrição |
| --------- | ----------- | --------- |
| `Content-Type` | Sim | `application/json` |
| `Idempotency-Key` | Não | Chave de até 100 caracteres. Repetir a requisição com a mesma chave devolve a ficha original, sem nova gravação nem nova chamada ao Gemini (RF06, HU07) |
| `X-Request-Id` | Não | Identificador para rastreio. Ecoado na resposta |

**Corpo: `CriarRelatoRequest`**

```csharp
public sealed record CriarRelatoRequest(string Relato);
```

| Campo | Tipo | Obrigatório | Regra |
| ----- | ---- | ----------- | ----- |
| `relato` | string | Sim | Não vazio após remover espaços. Até o limite configurado: provisório, 2.000 caracteres (P5 na especificação). Texto livre, tratado como dado (RF11) |

O corpo deve conter somente `relato`. Campo desconhecido, como `urgenciaFinal`, causa **400**.
Assim, o cliente não consegue alterar as regras de urgência nem a categoria.

**Exemplo**

```json
{
  "relato": "Honda CB 300 2017. O freio dianteiro está mole, preciso puxar bem a manete."
}
```

```bash
curl -i -X POST http://localhost:5000/api/triagem \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: balcao-2026-11-04-0001" \
  -d '{"relato":"Honda CB 300 2017. O freio dianteiro está mole, preciso puxar bem a manete."}'
```

### 4.2 Resposta: `FichaServicoResponse`

O mesmo formato é usado nas respostas 200 e 422. A diferença está nos campos preenchidos.

```csharp
public sealed record FichaServicoResponse(
    Guid IdPedido,
    Guid? IdOrdem,
    string Resultado,                 // "gravada" | "pergunta"
    string? ModeloMoto,
    int? Ano,
    IReadOnlyList<string> Sintomas,
    string Categoria,                 // uma das 8 categorias canônicas
    string UrgenciaLlm,               // "baixa" | "media" | "alta"
    string UrgenciaFinal,             // "baixa" | "media" | "alta"
    IReadOnlyList<string> PecasProvaveis,
    bool AvisoUrgencia,               // true quando UrgenciaFinal == "alta"
    IReadOnlyList<string> CamposFaltando,        // "modeloMoto" | "problema"
    string? PerguntaAoAtendente,
    IReadOnlyList<string> CamposNaoConfirmados,  // "modeloMoto" | "ano"
    string? MensagemCliente,          // até 60 palavras; null quando não gerada
    IReadOnlyList<string> Avisos      // ex.: "mensagem_nao_gerada"
);
```

| Campo | Quando vem preenchido |
| ----- | --------------------- |
| `idOrdem` | Somente com `resultado = "gravada"` |
| `modeloMoto`, `ano` | `null` quando o valor não aparece no relato (CT-13, CT-15) |
| `camposFaltando`, `perguntaAoAtendente` | Preenchidos somente com `resultado = "pergunta"` |
| `camposNaoConfirmados` | Lista de valores que o Gemini informou e que não aparecem no relato |
| `mensagemCliente` | Somente com `resultado = "gravada"` e mensagem gerada |
| `avisos` | Lista vazia quando tudo ocorreu bem |

**Perguntas de esclarecimento** vêm de uma tabela fixa no código, não do Gemini. Isso torna a
pergunta reprodutível:

| Campo faltando | `perguntaAoAtendente` |
| -------------- | --------------------- |
| `modeloMoto` | "Qual é o modelo da moto?" |
| `problema` | "Qual é o problema que o cliente descreve?" |

### 4.3 Respostas

**200 OK: ficha gravada (`resultado = "gravada"`)**

```json
{
  "idPedido": "6f1c2a9e-3b7d-4e8a-9c1f-2d5b7a8e4c10",
  "idOrdem": "9a4e7c2b-1d3f-4b6a-8e9d-0c2f5a7b1e33",
  "resultado": "gravada",
  "modeloMoto": "Honda CB 300",
  "ano": 2017,
  "sintomas": ["freio dianteiro mole", "preciso puxar bem a manete"],
  "categoria": "freios",
  "urgenciaLlm": "media",
  "urgenciaFinal": "alta",
  "pecasProvaveis": ["pastilhas de freio", "disco de freio"],
  "avisoUrgencia": true,
  "camposFaltando": [],
  "perguntaAoAtendente": null,
  "camposNaoConfirmados": [],
  "mensagemCliente": "Olá! Recebemos o pedido para a sua Honda CB 300, ano 2017. O serviço indicado é a verificação do sistema de freios. Como o caso envolve segurança, recomendamos não usar a moto até a avaliação do mecânico. Em breve enviaremos o orçamento. Equipe de atendimento",
  "avisos": []
}
```

Este exemplo corresponde ao caso CT-02. O Gemini respondeu `media`, e a palavra "freio"
elevou a urgência final para `alta`.

**422 Unprocessable Content: pergunta, nada gravado (`resultado = "pergunta"`)**

Usa o código 422 porque o relato chegou com formato correto, mas as regras de negócio não
permitem gravar a ordem (RF05). O nome do status no RFC 9110 é *Unprocessable Content*; a
documentação usa o termo "Unprocessable Entity", mais conhecido.

```json
{
  "idPedido": "3c7e1f0a-8b2d-4f5e-a6c9-1d4b8e2f7a05",
  "idOrdem": null,
  "resultado": "pergunta",
  "modeloMoto": null,
  "ano": 2017,
  "sintomas": ["barulho no motor", "perda de força"],
  "categoria": "motor e lubrificação",
  "urgenciaLlm": "media",
  "urgenciaFinal": "media",
  "pecasProvaveis": [],
  "avisoUrgencia": false,
  "camposFaltando": ["modeloMoto"],
  "perguntaAoAtendente": "Qual é o modelo da moto?",
  "camposNaoConfirmados": [],
  "mensagemCliente": null,
  "avisos": []
}
```

Corresponde ao caso CT-13. A ficha não é gravada, e o atendente reenvia o relato completo.

**400 Bad Request: contrato inválido** (`ValidationProblemDetails`)

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1",
  "title": "Um ou mais campos são inválidos.",
  "status": 400,
  "errors": {
    "relato": ["O campo relato é obrigatório e não pode ficar vazio."]
  },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

Causas: JSON malformado; campo `relato` ausente, vazio ou só com espaços; tamanho acima do limite configurado (provisório: 2.000 caracteres); campo desconhecido no corpo.

**415 Unsupported Media Type:** `Content-Type` diferente de `application/json`.

**429 Too Many Requests:** limite de taxa da própria API atingido (RNF10). Inclui `Retry-After`.

**502 Bad Gateway: saída da IA inválida após as tentativas** (`ProblemDetails` com extensões)

```json
{
  "type": "about:blank",
  "title": "A IA não devolveu uma ficha válida.",
  "status": 502,
  "detail": "Foram feitas 3 tentativas sem saída válida. Nada foi gravado. Envie o relato novamente.",
  "codigo": "saida_llm_invalida",
  "urgenciaRiscoDetectado": true,
  "idPedido": "b84d2e61-0f3a-4c9d-9e7b-5a1c6d8f2e40",
  "traceId": "00-9a1c3e5f7b2d4a6c8e0f1b3d5a7c9e2f-1a2b3c4d5e6f7a8b-00"
}
```

**503 Service Unavailable: IA ou banco indisponível** (`ProblemDetails`)

```json
{
  "type": "about:blank",
  "title": "Serviço de triagem temporariamente indisponível.",
  "status": 503,
  "detail": "O serviço de IA está no limite de uso. Tente novamente em alguns minutos. Nada foi gravado.",
  "codigo": "ia_indisponivel",
  "urgenciaRiscoDetectado": false,
  "idPedido": "e5f0a9c2-4d1b-4e7a-b3c8-9f2d6a1e7b58",
  "traceId": "00-1f3e5d7c9b2a4e6f8d0c1b3a5e7f9d2c-7e8f9a0b1c2d3e4f-00"
}
```

Cabeçalho `Retry-After` presente. Quando o problema é a chave inválida (9b), o código é
`ia_indisponivel`, sem detalhes sobre a chave.

Nos casos 502 e 503, `urgenciaRiscoDetectado` é `true` se o relato contém palavra de risco.
Assim, o atendimento mantém o alerta mesmo sem ficha (regra de segurança em falha da
[arquitetura](06-Arquitetura-solucao.md), seção 7.3).

**500 Internal Server Error:** erro não tratado. Não traz detalhes; o `traceId` permite
localizar o erro no log.

---

## 5. `POST /api/telegram/webhook`

Recebe as mensagens do bot do Telegram. O processamento é **síncrono**: o Telegram aguarda a
resposta HTTP, e a própria resposta carrega a mensagem do bot (*webhook reply*). Assim não é
preciso uma chamada extra à Bot API. A regra de triagem é a mesma do `POST /api/triagem`.

### 5.1 Configuração do webhook

O webhook é registrado uma vez, pela Bot API, com um segredo:

```
POST https://api.telegram.org/bot<TOKEN>/setWebhook
  url=<endereço público HTTPS da API>/api/telegram/webhook
  secret_token=<valor de Telegram:SecretToken>
  allowed_updates=["message"]
```

Em desenvolvimento local, o Telegram não alcança `localhost`. Por isso, é preciso um túnel
HTTPS para testar o canal de ponta a ponta. Os testes de integração simulam a chamada e não
dependem do túnel.

### 5.2 Requisição

**Cabeçalhos**

| Cabeçalho | Obrigatório | Descrição |
| --------- | ----------- | --------- |
| `X-Telegram-Bot-Api-Secret-Token` | Sim | Deve ser igual a `Telegram:SecretToken`. Comparação em tempo constante. Ausente ou diferente retorna **401** |
| `Content-Type` | Sim | `application/json` |

**Corpo: subconjunto do `Update` do Telegram**

```json
{
  "update_id": 748392011,
  "message": {
    "message_id": 311,
    "date": 1762200000,
    "chat": { "id": -1001234567890, "type": "private" },
    "text": "Honda CB 300 2017. O freio está mole."
  }
}
```

A API lê somente `update_id`, `message.chat.id` e `message.text`. Os campos de remetente
(`from`) são ignorados, para não guardar nome de cliente ou atendente (RNF02).

### 5.3 Tratamento

| Situação | Ação | Resposta HTTP | Corpo |
| -------- | ---- | ------------- | ----- |
| Segredo ausente ou inválido | Nada é processado | 401 | Vazio |
| JSON malformado ou sem `update_id` | Nada é processado | 400 | ProblemDetails |
| Mensagem sem texto (foto, áudio, figurinha) | Ignorada | 200 | Vazio, sem resposta no chat |
| Chat fora da lista autorizada (`Telegram:ChatsPermitidos`), inclusive com lista vazia | Ignorado e registrado no log, antes de qualquer chamada ao Gemini (fail closed) | 200 | Vazio, sem resposta no chat |
| `update_id` já processado | Ignorado (chave `telegram:{update_id}`) | 200 | Vazio, sem resposta no chat |
| Comando `/start` ou `/ajuda` | Responde com instruções fixas, sem chamar o Gemini | 200 | Resposta de mensagem |
| Relato válido | Executa o UC01 | 200 | Resposta de mensagem com a ficha |
| Relato com pergunta | Executa o UC01 | 200 | Resposta de mensagem com a pergunta |
| Falha de IA ou banco | Executa o UC01 | 200 | Resposta de mensagem com o erro |

**Ordem das verificações.** Segredo, depois chat autorizado, depois texto, e só então a triagem. Um update que não passa nas duas primeiras verificações não gera chamada ao Gemini nem registro de execução.

**Por que 200 nos casos ignorados.** Se a API respondesse 4xx, o Telegram reenviaria o
mesmo `update`, e o chat receberia respostas repetidas. O 401 é a exceção, porque indica
requisição que não vem do Telegram.

### 5.4 Resposta: formato de *webhook reply*

```json
{
  "method": "sendMessage",
  "chat_id": -1001234567890,
  "text": "✅ Ordem registrada (nº 9a4e7c2b)\nMoto: Honda CB 300 (2017)\nServiço: freios\nUrgência: ALTA\n\n⚠️ Urgência alta: avise o mecânico antes de atender o próximo pedido.\n\nMensagem ao cliente:\nOlá! Recebemos o pedido para a sua Honda CB 300, ano 2017. ..."
}
```

O texto é enviado sem formatação (sem `parse_mode`), para evitar erro de interpretação de
Markdown ou HTML. O limite de mensagem do Telegram é de 4.096 caracteres; a mensagem ao
cliente tem no máximo 60 palavras, então o limite nunca é atingido.

**Modelos de texto do bot**

- **Gravada:** ficha em linhas, com o aviso de urgência alta em destaque, se houver, e a
  mensagem ao cliente.
- **Pergunta:** "Não gravado. Falta informar: modelo da moto. Pergunta: Qual é o modelo da
  moto? Envie o relato completo de novo."
- **Erro:** "Não consegui processar agora. Nada foi gravado. Tente novamente em alguns
  minutos."

### 5.5 Ponto de atenção: processamento síncrono

O Telegram espera a resposta do webhook por tempo limitado. Se a triagem demorar mais, o
Telegram reenvia o mesmo `update`. A chave `telegram:{update_id}` impede a gravação duplicada,
mas o atendente pode ver a resposta atrasada ou repetida. Por isso, o tempo total do pedido
é limitado: 10 segundos por chamada ao Gemini e no máximo 3 tentativas de extração. A decisão
e a alternativa (fila de processamento em segundo plano) estão na
[arquitetura](06-Arquitetura-solucao.md), seções 5.3 e 7.2.

---

## 6. `GET /api/ordens-servico`

Lista ordens para o mecânico (UC02, HU08 e HU09).

### 6.1 Parâmetros de consulta

| Parâmetro | Tipo | Padrão | Regra |
| --------- | ---- | ------ | ----- |
| `urgenciaFinal` | string | (sem filtro) | `baixa`, `media` ou `alta` |
| `categoria` | string | (sem filtro) | Uma das 8 categorias canônicas, com ou sem acento. Ex.: `freios`, `pneus e rodas` |
| `status` | string | `aberta` | `aberta`, `em_execucao`, `concluida`, `cancelada` ou `todos` |
| `pagina` | inteiro | `1` | Mínimo 1 |
| `tamanhoPagina` | inteiro | `20` | Entre 1 e 100 |

**Ordenação fixa:** urgência final (`alta`, `media`, `baixa`) e, dentro de cada nível, data
de criação mais antiga primeiro. Essa ordem é a fila de atendimento.

### 6.2 Resposta 200

```json
{
  "pagina": 1,
  "tamanhoPagina": 20,
  "total": 2,
  "items": [
    {
      "idOrdem": "9a4e7c2b-1d3f-4b6a-8e9d-0c2f5a7b1e33",
      "modeloMoto": "Honda CB 300",
      "ano": 2017,
      "categoria": "freios",
      "sintomas": ["freio dianteiro mole", "preciso puxar bem a manete"],
      "urgenciaLlm": "media",
      "urgenciaFinal": "alta",
      "status": "aberta",
      "canalOrigem": "rest",
      "dataCriacao": "2026-11-04T14:05:00Z"
    },
    {
      "idOrdem": "d2b8f1e4-6a3c-4d9b-8f5e-7c1a0b3e9d26",
      "modeloMoto": "Honda Biz 125",
      "ano": 2014,
      "categoria": "pneus e rodas",
      "sintomas": ["pneu traseiro careca"],
      "urgenciaLlm": "alta",
      "urgenciaFinal": "alta",
      "status": "aberta",
      "canalOrigem": "telegram",
      "dataCriacao": "2026-11-04T14:20:00Z"
    }
  ]
}
```

Campos da lista não incluem `mensagemCliente`, `pecasProvaveis` e `camposNaoConfirmados`. Esses
aparecem somente no detalhe.

**Erros:** 400 para filtro inválido (`ValidationProblemDetails`). Lista vazia não é erro: a
resposta é 200 com `items: []` e `total: 0`.

---

## 7. `GET /api/ordens-servico/{id}`

Mostra a ficha completa de uma ordem (HU10).

**Parâmetro de rota:** `id`, UUID.

**Resposta 200:** o mesmo formato de item da lista, acrescido de `pecasProvaveis`,
`camposNaoConfirmados` e `mensagemCliente`.

**Resposta 404** (`ProblemDetails`):

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5",
  "title": "Ordem não encontrada.",
  "status": 404,
  "traceId": "00-5e7a9c1b3d5f7a9c1e3b5d7f9a1c3e5b-2c4e6a8c0e2a4c6e-00"
}
```

---

## 8. Erros: resumo e extensões

Todos os erros usam RFC 9457. As extensões abaixo aparecem quando se aplicam.

| Extensão | Tipo | Onde aparece | Significado |
| -------- | ---- | ------------ | ----------- |
| `codigo` | string | 502, 503 | Código estável para tratamento pelo cliente: `saida_llm_invalida`, `ia_indisponivel`, `banco_indisponivel` |
| `urgenciaRiscoDetectado` | boolean | 502, 503 | `true` quando o relato contém palavra de risco. O cliente deve alertar o atendimento |
| `idPedido` | UUID | 502, 503 | Identificador do pedido, para cruzar com o registro de execução |
| `errors` | objeto | 400 | Erros por campo (`ValidationProblemDetails`) |
| `traceId` | string | Todos | Para localizar o erro no log da API |

**Tabela de códigos HTTP**

| Código | Quando | Detalhe |
| ------ | ------ | ------- |
| 200 | Ficha gravada, consulta ok, webhook processado ou ignorado | Corpo varia por endpoint |
| 400 | Contrato inválido, filtro inválido, JSON malformado | `ValidationProblemDetails` |
| 401 | Segredo do webhook ausente ou inválido | Corpo vazio |
| 404 | Ordem inexistente | `ProblemDetails` |
| 415 | `Content-Type` não suportado | `ProblemDetails` |
| 422 | Pergunta de esclarecimento; nada gravado | `FichaServicoResponse` com `resultado = "pergunta"` |
| 429 | Limite de taxa da API | `ProblemDetails` com `Retry-After` |
| 500 | Erro não tratado | `ProblemDetails` sem detalhes internos |
| 502 | Saída da IA inválida após as tentativas | `ProblemDetails` com `codigo` |
| 503 | IA ou banco indisponível | `ProblemDetails` com `codigo` e `Retry-After` |

---

## 9. Segurança e limites da v1

- **Rede:** a API escuta em `127.0.0.1` na v1. Para o webhook do Telegram, a exposição
  pública ocorre por túnel HTTPS, somente durante os testes do canal.
- **Autenticação:** sem login de usuário na v1. A proteção do webhook é o segredo
  (`X-Telegram-Bot-Api-Secret-Token`) e a lista de chats autorizados, em modo fail closed (lista vazia ignora todos os chats). A autenticação dos
  endpoints REST fica para a evolução (ver [arquitetura](06-Arquitetura-solucao.md), seção 12).
- **Entrada:** contrato estrito (campos desconhecidos rejeitados), limite de tamanho e
  validação de tipos antes de qualquer processamento.
- **Segredos:** a chave do Gemini e o token do Telegram ficam em user-secrets ou variáveis de
  ambiente. Nenhum segredo aparece nas respostas, nos logs ou no repositório.
- **CORS:** desligado na v1, porque não há front-end publicado.
- **Limite de taxa:** aplicado por endereço de origem, para proteger a cota do Gemini.

---

## 10. Versionamento e compatibilidade

- Mudança compatível (campo novo opcional na resposta, novo filtro opcional) não altera a
  versão da rota.
- Mudança incompatível (campo removido, tipo alterado, código de status trocado) exige nova
  rota, por exemplo `/api/v2/triagem`, e atualização do `openapi.yaml`.
- A versão do prompt (`v1`, `v2`) não aparece no contrato da API. Ela fica no registro de
  execução (RF10), para que a API não precise mudar a cada ajuste do prompt.


---

## 11. DTOs de referência

Os records abaixo são o formato de entrada e saída da API. Os nomes JSON seguem `camelCase`, exceto
no webhook do Telegram, cujos records usam `[JsonPropertyName]` para os nomes `snake_case` da Bot API.

```csharp
// Entrada: POST /api/triagem
public sealed record CriarRelatoRequest(string Relato);

// Saída: POST /api/triagem (200 e 422). Definido na seção 4.2.
public sealed record FichaServicoResponse(
    Guid IdPedido, Guid? IdOrdem, string Resultado,
    string? ModeloMoto, int? Ano, IReadOnlyList<string> Sintomas,
    string Categoria, string UrgenciaLlm, string UrgenciaFinal,
    IReadOnlyList<string> PecasProvaveis, bool AvisoUrgencia,
    IReadOnlyList<string> CamposFaltando, string? PerguntaAoAtendente,
    IReadOnlyList<string> CamposNaoConfirmados, string? MensagemCliente,
    IReadOnlyList<string> Avisos);

// Consulta: GET /api/ordens-servico (itens da lista)
public sealed record OrdemServicoResumo(
    Guid IdOrdem, string? ModeloMoto, int? Ano, string Categoria,
    IReadOnlyList<string> Sintomas, string UrgenciaLlm, string UrgenciaFinal,
    string Status, string CanalOrigem, DateTimeOffset DataCriacao);

// Consulta: GET /api/ordens-servico/{id} (acrescenta os campos do detalhe)
public sealed record OrdemServicoDetalhe(
    Guid IdOrdem, string? ModeloMoto, int? Ano, string Categoria,
    IReadOnlyList<string> Sintomas, string UrgenciaLlm, string UrgenciaFinal,
    string Status, string CanalOrigem, DateTimeOffset DataCriacao,
    IReadOnlyList<string> PecasProvaveis, IReadOnlyList<string> CamposNaoConfirmados,
    string? MensagemCliente);

// Consulta: página de ordens
public sealed record PaginaOrdens(
    int Pagina, int TamanhoPagina, int Total, IReadOnlyList<OrdemServicoResumo> Items);

// Entrada: POST /api/telegram/webhook (subconjunto do Update)
public sealed record TelegramUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMensagem? Message);

public sealed record TelegramMensagem(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("date")] long Date,
    [property: JsonPropertyName("chat")] TelegramChat Chat,
    [property: JsonPropertyName("text")] string? Text);

public sealed record TelegramChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type);

// Saída: webhook reply (*webhook reply* da Bot API)
public sealed record TelegramWebhookReply(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("chat_id")] long ChatId,
    [property: JsonPropertyName("text")] string Text);
```

**Regra de nomes.** O campo `From` do `Update` do Telegram não é mapeado. Assim, o nome do cliente
ou do atendente nunca chega ao modelo de dados (RNF02).