# 02 — Especificação

- **Versão:** 2.1
- **Data da última mudança:** 05/11/2026
- **Substitui:** versão 1.0 (30/10/2026), que descrevia o fluxo em n8n

> Esta versão adapta o projeto *Triagem de Ordens de Serviço* para uma API REST em
> C# / .NET. As regras de negócio se mantêm: as 8 categorias canônicas, a validação
> determinística em código e a regra de piso de urgência por palavras de risco. Mudam o
> canal de entrada, a orquestração (n8n sai do caminho de execução) e a persistência
> (PostgreSQL no lugar de planilha ou CSV).

---

## 1. Modelo de negócio

### 1.1 Proposta de valor

- **Para a oficina:** pedidos registrados com dados completos e urgência definida antes de
  chegarem ao mecânico, consultáveis por API.
- **Para o cliente:** resposta padronizada e mais rápida ao relato do problema.
- **Para o portfólio:** demonstração de API em .NET, integração com LLM, validação
  determinística de saída, persistência em PostgreSQL e medição objetiva de qualidade.

### 1.2 Segmento

Oficinas pequenas e médias de motos, com um atendente no balcão e poucos mecânicos. Não
atende oficinas de carros, concessionárias nem frotas.

### 1.3 Canais de atendimento

| Canal | Quem usa | Ponto de entrada |
| ----- | -------- | ---------------- |
| Cliente REST | Atendimento (Carla), por aplicação própria ou ferramenta de teste | `POST /api/triagem` |
| Bot do Telegram | Atendimento (Carla), pelo chat do bot | `POST /api/telegram/webhook` |
| Consulta de ordens | Mecânico (Antônio), por consumo de API | `GET /api/ordens-servico` |

### 1.4 Fora do escopo da versão 1

- Interface visual própria. A versão 1 é somente API.
- Cobrança, preços, orçamento automático e integração com sistema de gestão da oficina.
- Envio automático ao cliente por WhatsApp. A mensagem continua sendo enviada pelo
  atendimento.
- Atualização de status das ordens. Toda ordem nasce com status `aberta`; as transições
  ficam para evolução futura.
- Autenticação de usuários. A versão 1 roda em rede local (ver RNF10).
- Lista de peças consultada por agente (RF12, versão 2).

---

## 2. Personas

As personas são fictícias e se inspiram na rotina de uma oficina de motos. Elas mudaram
em relação à versão 1: a interação deixa de ser um formulário do n8n e passa a ser feita
por clientes da API.

> ### Carla — atendente
>
> **Função:** recebe o pedido no balcão ou pelo telefone e o registra no sistema.
>
> **Rotina:** atende de dez a vinte pedidos por dia, com interrupções constantes.
>
> **Necessidade:** registrar o relato rápido, sem esquecer modelo ou sintoma, e saber de
> imediato se o caso é de risco.
>
> **Dor:** anota o problema "do jeito que o cliente fala" e às vezes esquece o ano da moto
> ou o detalhe que indica risco.
>
> **Familiaridade técnica:** usa planilha e aplicativo de mensagens. Não programa.
>
> **Interage com o sistema por:** cliente REST (aplicação de balcão, em evolução) ou bot do
> Telegram, que recebe o relato e responde com a ficha, a pergunta ou o aviso de urgência.

> ### Antônio — mecânico
>
> **Função:** executa os serviços e decide a ordem de atendimento.
>
> **Rotina:** consulta as ordens abertas no início do turno.
>
> **Necessidade:** saber, sem interpretar texto, o que fazer, em qual moto e com qual
> urgência.
>
> **Dor:** fichas vagas obrigam a ligar para o cliente antes de começar o serviço.
>
> **Familiaridade técnica:** usa planilha básica no celular.
>
> **Interage com o sistema por:** consulta de ordens com filtros de urgência e categoria
> (UC02). Enquanto não houver tela própria, pode consumir o endpoint por uma ferramenta que
> aceite JSON.

> ### Rodrigo — cliente
>
> **Função:** leva a moto para revisão ou conserto.
>
> **Necessidade:** entender o que será feito e quando recebe o orçamento.
>
> **Dor:** recebe respostas genéricas e demora para saber se a moto está segura para rodar.
>
> **Familiaridade técnica:** usa aplicativo de mensagens. Não acessa o sistema diretamente.
>
> **Interage com o sistema por:** mensagem escrita pelo sistema e enviada pelo atendente.

> ### Eduardo — desenvolvedor e avaliador
>
> **Função:** constrói, testa e documenta a API. Também é o avaliador técnico da solução.
>
> **Necessidade:** medir a precisão do LLM com números, comparar versões do prompt e
> reproduzir qualquer execução.
>
> **Dor:** resultados que não podem ser reproduzidos por quem lê o portfólio.
>
> **Familiaridade técnica:** programa em nível intermediário e revisa código com frequência.
>
> **Interage com o sistema por:** repositório, suíte de testes automatizados, contrato
> OpenAPI, PostgreSQL em contêiner Docker e relatórios de avaliação.

---

## 3. Histórias de usuário

Cada história traz critérios de aceite que apontam para um requisito da seção 5.

### 3.1 Atendente (Carla)

| ID | História | Critérios de aceite | Requisitos |
| -- | -------- | ------------------- | ---------- |
| HU01 | Como atendente, quero enviar o relato do cliente em um único campo, para registrar o pedido sem formulários longos. | Contrato com um campo obrigatório (`relato`); relato vazio retorna 400 | RF01, RNF06 |
| HU02 | Como atendente, quero receber a ficha estruturada logo após o envio, para conferir os dados antes de passar ao mecânico. | Resposta 200 com modelo, ano, sintomas, categoria, urgência e peças prováveis | RF02, RF03 |
| HU03 | Como atendente, quero receber uma pergunta quando faltar modelo ou problema, para completar o pedido na hora. | Resposta 422 com `perguntaAoAtendente` e `camposFaltando`; nada gravado | RF05, RF06 |
| HU04 | Como atendente, quero um aviso destacado quando a urgência for alta, para tratar o caso antes dos demais. | `avisoUrgencia` verdadeiro sempre que a urgência final for `alta`; no Telegram, aviso em destaque | RF04 |
| HU05 | Como atendente, quero receber uma mensagem pronta para o cliente, para enviá-la sem escrever do zero. | Mensagem em português com até 60 palavras, citando moto e serviço | RF07 |
| HU06 | Como atendente, quero uma resposta clara quando o serviço de IA estiver indisponível, para saber que devo tentar mais tarde. | 503 com `Retry-After` e corpo ProblemDetails legível | RF09 |
| HU07 | Como atendente, quero repetir um envio sem criar duplicata, para não registrar o mesmo pedido duas vezes. | Mesma chave de idempotência devolve a ficha existente, sem nova gravação | RF06 |

### 3.2 Mecânico (Antônio)

| ID | História | Critérios de aceite | Requisitos |
| -- | -------- | ------------------- | ---------- |
| HU08 | Como mecânico, quero ver cada pedido já classificado por categoria, para saber qual serviço preparar. | Categoria é um dos oito valores permitidos | RF03, RF13 |
| HU09 | Como mecânico, quero filtrar as ordens por urgência, para atender primeiro os casos de risco. | Filtro por urgência; lista ordenada com `alta` primeiro | RF04, RF13 |
| HU10 | Como mecânico, quero abrir uma ordem pelo identificador, para ver a ficha completa. | GET por id retorna a ficha; id inexistente retorna 404 | RF13 |

### 3.3 Cliente (Rodrigo)

| ID | História | Critérios de aceite | Requisitos |
| -- | -------- | ------------------- | ---------- |
| HU11 | Como cliente, quero uma resposta clara sobre o que será feito, para entender o atendimento. | Mensagem cita a moto e o serviço, sem prometer preço | RF07 |

### 3.4 Desenvolvedor e avaliador (Eduardo)

| ID | História | Critérios de aceite | Requisitos |
| -- | -------- | ------------------- | ---------- |
| HU12 | Como desenvolvedor, quero que a avaliação use o mesmo núcleo da API, para que a medição reflita o uso real. | Testes de avaliação chamam o serviço de aplicação usado pelo endpoint | RF08 |
| HU13 | Como avaliador, quero ver a versão do prompt, o modelo e a temperatura de cada execução, para comparar resultados. | Cinco campos de rastreio em todo registro de execução | RF10, RNF11 |
| HU14 | Como desenvolvedor, quero que um texto que tente mudar as regras seja tratado como dado, para proteger a triagem. | Casos com instruções embutidas no relato são ignorados | RF11 |
| HU15 | Como avaliador, quero que nenhuma chave de API apareça no repositório, para publicar o projeto com segurança. | Chave somente em user-secrets ou variáveis de ambiente; nenhum segredo em `appsettings*.json` versionado | RNF02, RNF05 |
| HU16 | Como desenvolvedor, quero que o contrato OpenAPI acompanhe o código, para que clientes não dependam de documentação desatualizada. | Teste automatizado compara o documento gerado com `docs/openapi.yaml` | RF15, RNF12 |

---

## 4. Casos de uso

Os casos de uso descrevem o comportamento esperado de cada ator. Os componentes citados
são os do [documento de arquitetura](06-Arquitetura-solucao.md). Os contratos HTTP estão em
[04-Projeto-interface.md](04-Projeto-interface.md).

### 4.1 UC01 — Registrar pedido de serviço via API

| Campo | Descrição |
| ----- | --------- |
| Ator principal | Atendente (Carla), pelo cliente REST ou pelo bot do Telegram |
| Ator secundário | API do Gemini (extração e redação); PostgreSQL |
| Gatilho | `POST /api/triagem` ou `POST /api/telegram/webhook` com mensagem de texto |
| Pré-condições | API em execução; migrações aplicadas; PostgreSQL acessível; chave do Gemini configurada |
| Entrada | `CriarRelatoRequest` (REST) ou `Update` do Telegram com texto. O relato é tratado como dado |
| Saída | 200 com ficha gravada; 422 com pergunta; 400, 502 ou 503 em caso de erro. No Telegram, a mesma informação vai como resposta do chat |
| Pós-condição | Ordem gravada somente se todas as validações passaram. A execução é registrada em todos os casos, inclusive nas falhas |
| Componentes | Endpoint, validação de contrato, serviço de aplicação, núcleo de regras (domínio), cliente do Gemini, repositórios EF Core |

**Fluxo normal**

1. O cliente envia o relato. Exemplo: `{ "relato": "Honda CB 300 2017. O freio está mole." }`.
2. A camada de entrada valida o contrato: JSON bem formado, campo `relato` presente, texto
   não vazio após remover espaços, tamanho até o limite configurado (provisório: 2.000 caracteres, ver P5) e nenhum campo
   desconhecido. Falha retorna **400** e nada mais é executado.
3. O serviço de aplicação gera o `idPedido` e verifica a chave de idempotência
   (cabeçalho `Idempotency-Key` ou `telegram:{update_id}`). Se já existir ordem com essa
   chave, devolve a ficha existente com **200**, sem nova chamada ao Gemini (ver UC01, fluxo
   alternativo 3c).
4. O núcleo de regras detecta palavras de risco no relato normalizado. Essa etapa não
   depende da resposta do modelo.
5. O cliente do Gemini envia o relato delimitado como dado, junto com a instrução de
   versão `vN` e o esquema JSON. A resposta bruta é recebida.
6. O validador de saída confere o JSON, o esquema, os tipos e a categoria (verificação 1).
   Chaves desconhecidas são descartadas e registradas. A categoria é normalizada para a
   forma canônica.
7. O núcleo confirma modelo e ano no texto do relato. Valor que não aparece no texto vira
   `null` e entra em `camposNaoConfirmados`.
8. O núcleo verifica os campos obrigatórios: modelo da moto e descrição do problema
   (verificação 2).
9. O núcleo aplica o piso de urgência: se houver palavra de risco, a urgência final é
   `alta`; caso contrário, vale a urgência do Gemini.
10. Com todas as verificações aprovadas, a API grava em uma única transação a ordem, os
    sintomas e o registro de execução vinculado à ordem. A chave de idempotência tem
    restrição `UNIQUE`, o que protege contra duas requisições simultâneas.
11. A API pede ao Gemini a mensagem ao cliente, com até 60 palavras, em uma execução
    registrada como `redacao_mensagem`. O código conta as palavras e refaz a redação
    no máximo duas vezes se passar do limite.
12. A API responde **200** com `FichaServicoResponse` (resultado `gravada`).

**Fluxos alternativos**

- **2a. Contrato inválido:** resposta 400 (`ValidationProblemDetails`). Nada é gravado e a
  execução não é registrada, porque o pedido não chegou ao núcleo.
- **3a. Campo fora do contrato, como `urgenciaFinal` enviado pelo cliente:** resposta 400.
  A urgência nunca vem do cliente.
- **3b. Relato com instrução para mudar as regras:** o texto segue como dado. Não há
  caminho especial; a regra vale para todos os pedidos (RF11).
- **3c. Chave de idempotência já usada:** resposta 200 com a ficha original. Para o
  Telegram, o `update` repetido não gera nova resposta no chat.
- **6a. Saída fora do esquema ou categoria inválida:** nova chamada ao Gemini, até 2 vezes
  (3 tentativas no total). Cada tentativa gera um registro de execução. Se a última
  tentativa falhar, o resultado é `falha_formato`: nada é gravado e a resposta é **502**
  (`saida_llm_invalida`). Se houver palavra de risco, a resposta traz a extensão
  `urgenciaRiscoDetectado: true`, para que o atendimento não perca o alerta.
- **8a. Modelo ou descrição do problema ausente:** o núcleo monta a pergunta, o resultado é
  `pergunta`, a ordem não é gravada e a resposta é **422** com `camposFaltando` e
  `perguntaAoAtendente`. Para reenviar, o atendente manda o relato completo (novo
  `POST`, sem depender de memória entre chamadas).
- **9a. Limite de cota (429), erro 5xx ou tempo esgotado no Gemini:** a biblioteca de
  resiliência repete a chamada HTTP até 2 vezes, com espera exponencial e respeito ao
  `Retry-After`. Se persistir, o resultado é `falha_api` e a resposta é **503** com
  `Retry-After`. Vale a mesma extensão de risco do item 6a.
- **9b. Chave do Gemini inválida (401 ou 403):** sem repetição. O erro é registrado no log
  da aplicação como crítico e a resposta é **503** com mensagem genérica. A chave nunca
  aparece na resposta nem no log.
- **9c. PostgreSQL indisponível:** a gravação falha antes de qualquer resposta de sucesso.
  A resposta é **503**. Se houver como registrar a execução, ela é registrada como
  `falha_api` com `codigo_erro = banco_indisponivel`.
- **11a. Falha na redação da mensagem:** a ficha continua gravada e a resposta é **200**
  com `mensagemCliente: null` e `avisos: ["mensagem_nao_gerada"]`. A gravação não é
  desfeita por causa do texto para o cliente.

**Regra de ordem.** A confirmação de modelo e ano (passo 7) acontece antes do enriquecimento
de marca por tabela estática. Na versão 1.0, o enriquecimento vinha antes. Assim, "CG 160"
virava "Honda CG 160" e a confirmação descartava o dado por não aparecer no relato. Veja
seção 6.4, regra 9.

### 4.2 UC02 — Consultar ordens de serviço

| Campo | Descrição |
| ----- | --------- |
| Ator principal | Mecânico (Antônio), por consumo de API |
| Gatilho | `GET /api/ordens-servico` ou `GET /api/ordens-servico/{id}` |
| Pré-condições | API em execução e base com ordens gravadas pelo UC01 |
| Entrada | Filtros opcionais: `urgenciaFinal`, `categoria`, `status`; paginação `pagina` e `tamanhoPagina` (máximo 100) |
| Saída | Página de ordens (200), ficha completa (200), lista vazia (200) ou não encontrado (404) |
| Pós-condição | Somente leitura. Nenhum dado é alterado |
| Componentes | Endpoint de consulta, serviço de consulta, repositório de ordens (EF Core), índice de fila |

**Fluxo normal**

1. O mecânico consulta `GET /api/ordens-servico?urgenciaFinal=alta&status=aberta`.
2. A API valida os filtros. Valor fora da lista retorna 400.
3. A API devolve a página ordenada por urgência (alta, média, baixa) e, dentro de cada
   nível, pela data de criação mais antiga primeiro. Quem chegou antes é atendido antes.
4. Para ver uma ficha completa, o mecânico usa `GET /api/ordens-servico/{id}`.

**Fluxos alternativos**

- **2a. Filtro inválido** (por exemplo, `urgenciaFinal=critica`): 400.
- **3a. Nenhuma ordem encontrada:** 200 com `items` vazio e `total: 0`.
- **4a. Identificador inexistente:** 404 (`ProblemDetails`).

### 4.3 UC03 — Executar testes automatizados

| Campo | Descrição |
| ----- | --------- |
| Ator principal | Desenvolvedor e avaliador (Eduardo) |
| Gatilho | `dotnet test` localmente ou no pipeline de integração contínua |
| Pré-condições | .NET SDK instalado; Docker ativo para os testes de integração (PostgreSQL em contêiner) |
| Entrada | Suíte de testes; gabarito `tests/casos_teste.csv` com 24 casos; configuração do Gemini somente para a avaliação |
| Saída | Resultado de cada teste; métricas do gabarito por versão do prompt (relatório separado) |
| Pós-condição | Nenhuma chamada paga ao Gemini ocorre sem opção explícita. Resultados de avaliação ficam fora do Git |
| Componentes | Projetos de teste unitário, de integração e de avaliação |

**Suítes**

| Suíte | O que cobre | Chamadas externas |
| ----- | ----------- | ----------------- |
| Unitária (`OsTriageApi.Tests.Unit`) | Categorias canônicas, piso de risco, confirmação de dados, campos obrigatórios, validação de saída, limite de palavras, instruções no relato (CT-23 e CT-24) com LLM simulado | Nenhuma |
| Integração (`OsTriageApi.Tests.Integration`) | Códigos HTTP de cada endpoint, idempotência, duplicidade, filtros e ordenação da consulta, webhook do Telegram. PostgreSQL em contêiner e LLM simulado | Nenhuma |
| Contrato | O documento OpenAPI gerado pela API é igual a `docs/openapi.yaml` | Nenhuma |
| Avaliação (`OsTriageApi.Tests.Avaliacao`) | Os 24 casos, três execuções cada, com Gemini real. Só roda com a variável `AVALIACAO_HABILITADA=true` | Gemini (72 chamadas por rodada) |

**Fluxo normal**

1. O desenvolvedor executa `dotnet test` nas suítes unitária, de integração e de contrato.
   Todas precisam passar antes de qualquer commit.
2. Para a avaliação, o desenvolvedor define a variável de habilitação e executa a suíte de
   avaliação. Ela lê os 24 casos e chama o mesmo serviço de aplicação do endpoint (RF08).
3. Cada caso roda três vezes, em lotes com pausa para respeitar o limite do nível gratuito.
4. A suíte grava um registro por chamada e calcula as métricas do
   [plano de testes](07-Plano-testes-software.md) por versão do prompt.

**Fluxos alternativos**

- **1a. Teste de integração sem Docker:** a suíte é marcada como ignorada, com aviso, em vez
  de falhar.
- **3a. Cota esgotada no meio da rodada:** a rodada para, grava o que foi obtido e o
  relatório indica os casos pendentes. A rodada é retomada no dia seguinte.

---

## 5. Requisitos

### 5.1 Requisitos funcionais

A prioridade segue a ordem de entrega. Cada requisito tem uma forma concreta de verificação.

| ID | Requisito | Prioridade | Como verificar |
| -- | --------- | ---------- | -------------- |
| RF01 | Receber o relato em texto livre por `POST /api/triagem` | Alta | Teste de integração: relato válido gera 200 ou 422; relato vazio gera 400 |
| RF02 | Extrair modelo da moto, ano, sintomas, categoria, urgência e peças prováveis em JSON | Alta | JSON válido no esquema em 100% das execuções do gabarito, após retentativas |
| RF03 | Classificar o serviço em uma das 8 categorias; valor fora da lista é rejeitado em código | Alta | Teste unitário com categoria simulada inválida; acerto medido pelo gabarito |
| RF04 | Classificar a urgência como baixa, média ou alta; uma regra em código eleva para alta quando o relato cita palavra de risco | Alta | Nenhum caso de risco do gabarito fica abaixo de alta; testes unitários da regra |
| RF05 | Calcular em código os campos obrigatórios ausentes e gerar a pergunta de esclarecimento | Alta | Casos CT-13 a CT-15 e CT-21 e CT-22 retornam 422 |
| RF06 | Gravar a ordem somente depois de todas as validações, com uma única linha por pedido, mesmo em novas tentativas | Média | Teste de integração: a mesma chave enviada duas vezes gera uma ordem |
| RF07 | Gerar mensagem ao cliente em português, com até 60 palavras | Média | Teste unitário do contador de palavras; conferência de moto e serviço |
| RF08 | Executar a avaliação pelo mesmo serviço de aplicação do endpoint | Média | Revisão de código: a suíte de avaliação não reimplementa regras |
| RF09 | Tratar falha da API externa (cota, tempo esgotado, chave inválida) com repetição controlada e erro claro | Média | Teste de integração com LLM simulado que responde 429 e 401 |
| RF10 | Registrar em cada execução a versão do prompt, o modelo, a temperatura, o tempo e a resposta bruta | Média | Todos os registros de execução têm esses campos preenchidos |
| RF11 | Tratar o relato do cliente como dado, nunca como instrução | Média | Casos CT-23 e CT-24 com LLM real e simulado |
| RF12 | (Versão 2) Consultar uma lista de peças por meio de um agente | Baixa | Ficha com peças vindas da lista |
| RF13 | Consultar ordens com filtros, ordenação por urgência e paginação, e consultar uma ordem pelo identificador | Média | Testes de integração dos filtros e da ordem da fila |
| RF14 | Receber mensagens do bot do Telegram de forma síncrona, responder com ficha, pergunta ou erro, e aceitar apenas chats autorizados | Média | Teste de integração com token válido, token inválido, chat fora da lista e lista vazia (todos os chats ignorados) |
| RF15 | Publicar o contrato OpenAPI da API, mantido sincronizado com o código | Média | Teste de contrato |

### 5.2 Requisitos não funcionais

As metas numéricas são propostas e podem ser ajustadas após a primeira rodada de testes.

| ID | Requisito | Meta |
| -- | --------- | ---- |
| RNF01 | Custo | R$ 0: PostgreSQL e .NET em contêiner ou instalação local gratuitos; nível gratuito do Gemini |
| RNF02 | Privacidade | Somente dados fictícios nos testes e na avaliação. Logs da aplicação não contêm relato, chave nem token. O banco guarda apenas o HMAC-SHA256 do relato, com chave secreta fora do banco, e a resposta bruta tem prazo de retenção |
| RNF03 | Confiabilidade da saída | JSON válido e dentro do esquema em 100% das execuções, com até 2 novas tentativas |
| RNF04 | Desempenho | Até 15 segundos por pedido no caminho feliz, de ponta a ponta (meta a validar; depende da API) |
| RNF05 | Reprodutibilidade | `docker-compose.yml` com PostgreSQL em versão fixada; migrações versionadas; prompts versionados; testes executáveis com um comando |
| RNF06 | Usabilidade do contrato | Pedido com um único campo obrigatório |
| RNF07 | Manutenibilidade | Prompt versionado (`prompts/v1.md`, `v2.md`) no repositório |
| RNF08 | Separação de responsabilidades | Regras de negócio e validações no domínio; o LLM só extrai, classifica e redige |
| RNF09 | Testabilidade | Núcleo de regras sem dependência de rede, banco ou framework web; LLM atrás de interface |
| RNF10 | Segurança da API | Validação de contrato; rejeição de campos desconhecidos; limite de tamanho do relato; token secreto no webhook do Telegram; limite de taxa por origem. Sem autenticação de usuário na v1, com a API restrita à rede local |
| RNF11 | Rastreabilidade | Cada chamada ao LLM gera um registro com versão do prompt, modelo, temperatura, tempo e resposta bruta (RF10) |
| RNF12 | Documentação de contrato | Documento OpenAPI 3.0 em `docs/openapi.yaml`, validado por teste automatizado |

### 5.3 Restrições

- **Gratuidade:** a solução roda sem custo; não há licença paga nem hospedagem.
- **Limite de requisições:** o nível gratuito do Gemini limita chamadas por minuto (RPM),
  por tokens por minuto (TPM) e por dia (RPD). Os limites são por projeto, e o contador diário
  zera à meia-noite do horário do Pacífico. Os testes de avaliação rodam em lotes com pausa.
- **Ambiente local:** a API e o PostgreSQL rodam na máquina do desenvolvedor. A API escuta
  apenas em `localhost`.
- **Dados:** somente dados fictícios.
- **Stack:** C# com .NET (versão LTS vigente no início do projeto), ASP.NET Core, EF Core com
  Npgsql e PostgreSQL. As escolhas estão na [arquitetura](06-Arquitetura-solucao.md), seção 5.

---

### 5.4 Estrutura de implementação

- **API:** Minimal APIs, com um grupo de endpoints por recurso (`TriagemEndpoints`,
  `TelegramWebhookHandler`, `ConsultaOrdensEndpoints`). Não há controllers.
- **DTOs:** records imutáveis para entrada e saída, como `CriarRelatoRequest`,
  `FichaServicoResponse`, `OrdemServicoResumo`, `PaginaOrdens` e `TelegramUpdate`. Os formatos estão
  em [04-Projeto-interface](04-Projeto-interface.md), seção 11.
- **Validação em duas camadas.** O contrato (formato, tamanho e campos desconhecidos) é validado na
  entrada, antes de qualquer processamento. As regras de negócio (categoria, risco, campos
  obrigatórios e confirmação de dados) são determinísticas, ficam no domínio e não dependem do Gemini.
- **Erros:** RFC 9457 (ProblemDetails), com códigos estáveis e extensões descritas na seção 8 da
  [interface](04-Projeto-interface.md).
- **Contrato:** `docs/openapi.yaml` é a referência. Um teste de contrato compara o documento gerado
  pela API com esse arquivo.
- **Configuração:** modelo, limites e chats autorizados ficam em configuração, nunca no código.
  Segredos ficam em user-secrets ou variáveis de ambiente.

---

## 6. Regras de negócio

### 6.1 Categorias de serviço

O sistema trabalha com oito categorias canônicas. A forma canônica é a que aparece na
API, no banco e na documentação:

1. `corrente e relação`
2. `freios`
3. `suspensão`
4. `motor e lubrificação`
5. `elétrica e bateria`
6. `pneus e rodas`
7. `revisão periódica`
8. `outro ou não identificado`

Relato que não trata de serviço de moto vai para `outro ou não identificado`. A comparação
com a saída do modelo ignora maiúsculas, espaços extras e acentos, mas a gravação usa sempre
a forma canônica.

### 6.2 Níveis de urgência

| Urgência | Valor na API | Critério | Exemplo |
| -------- | ------------ | -------- | ------- |
| Alta | `alta` | Risco à segurança de quem pilota | Freio fraco, vazamento de óleo, folga na direção |
| Média | `media` | Afeta o uso, mas a moto ainda roda | Ruído na corrente, partida difícil |
| Baixa | `baixa` | Manutenção preventiva ou estética | Revisão de rotina, troca de retrovisor |

Os valores são em minúsculas e sem acento, como no esquema da versão 1.0.

### 6.3 Campos obrigatórios

- **Modelo da moto** e **descrição do problema** são obrigatórios. A descrição é considerada
  presente quando há pelo menos um sintoma.
- **Ano** é desejável, mas a sua falta não bloqueia a gravação.

### 6.4 Regras de decisão

1. Se faltar campo obrigatório, o núcleo detecta, a API responde **422** com a pergunta e
   **não grava** a ordem. A categoria `outro ou não identificado` não exige campo de detalhe:
   a ficha é gravável quando contém modelo da moto e ao menos um sintoma com texto.
2. Se modelo ou ano não aparecerem no texto do relato, o campo fica `null`. O sistema nunca
   deduz nem inventa.
3. Palavras de risco no relato elevam a urgência final para `alta`, mesmo que o Gemini tenha
   respondido outra coisa. A urgência nunca é reduzida.
4. Urgência final `alta` sempre sinaliza o aviso (`avisoUrgencia: true`).
5. Categoria fora das oito é rejeitada como falha de formato e dispara nova tentativa de
   extração, até 2 vezes.
6. Instruções escritas dentro do relato são ignoradas. O texto do cliente é sempre dado.
7. Falha após as tentativas resulta em **502** ou **503**, nada é gravado e a execução é
   registrada. Se houver palavra de risco, a resposta traz `urgenciaRiscoDetectado: true`.
8. Pedido repetido com a mesma chave de idempotência devolve a ficha existente e não gera
   nova ordem.
9. **Nota de extração e enriquecimento.** O Gemini extrai o modelo estritamente como
   redigido no relato. O enriquecimento de marca, como inferir "Honda" a partir de "CG 160",
   é feito depois da confirmação, em código, por uma tabela estática (`TabelaMarcas`).
   Modelo não mapeado passa sem alteração. O Gemini nunca completa o modelo com marca ou
   outro dado que não esteja no texto.

### 6.5 Palavras de risco (lista inicial)

- freio;
- vazamento ou perda de óleo;
- folga na direção;
- pneu furado ou careca;
- moto desligando sozinha.

A detecção normaliza o texto (minúsculas e sem acento) antes de procurar as palavras. Essas
palavras não substituem o Gemini: elas apenas elevam a urgência para `alta`. A lista será
revisada com os erros dos testes (ver 6.6).

#### Proposta de ampliação (a validar)

Esta proposta **não está em vigor**. A lista atual continua valendo até a proposta passar nos testes unitários e em uma rodada da avaliação. As mudanças são feitas uma por vez, como na seção 2.2 da metodologia.

Padrões propostos, somados aos atuais:

- `vaz\w*\s+(de\s+)?oleo`: cobre "vaza óleo", "vazando óleo" e "vazamento de óleo";
- `pneu\b.{0,30}\b(furad\w*|care(ca|qu)\w*)`: cobre "pneu traseiro está careca" e "pneu tá carequinha".

Conferência sobre os 24 relatos do gabarito, com os textos normalizados:

- a lista atual detecta 6 dos 9 casos de risco;
- a proposta detecta os 9, incluindo CT-06, CT-19 e CT-20;
- nenhum caso sem risco do gabarito passa a disparar alerta.

**Falso positivo conhecido:** "pneu novo, sem careca" dispara a proposta. Antes de adotar, decidir
se o custo é aceitável (um alerta a mais, que pesa na métrica "falsos alertas", com meta de 10%
ou menos) ou se a negação precisa de tratamento próprio.

### 6.6 Pendências de regra

Estes pontos precisam de decisão antes da fase de testes. Eles foram identificados ao
comparar a lista de palavras de risco com o gabarito.

Status: P3, P4 e P5 adotadas; retenção de 30 dias aprovada como baseline; P1 não adotada e mantida apenas como proposta a validar (seção 6.5); P2 aguardando confirmação.

| ID | Pendência | Efeito hoje | Opções |
| -- | --------- | ----------- | ------ |
| P1 | A lista não cobre todas as formulações do gabarito. "pneu **traseiro** está careca" (CT-06) não casa com `pneu (furado\|careca)`. "vaza óleo do motor" (CT-19) não casa com "vazamento ou perda de óleo". "carequinha" (CT-20) também não é coberta | Nesses casos, a urgência alta depende só do Gemini. Isso contraria a regra de piso | (a) ampliar a detecção, por exemplo aceitando "pneu" seguido de "careca" ou "furado" em até poucas palavras, e incluir "vaza óleo" e "carequinha"; (b) manter a lista e aceitar a lacuna, medindo-a no CT-06, CT-19 e CT-20. **Proposta a validar** (seção 6.5, opção a), com testes unitários para cada formulação. Decisão pendente |
| P2 | Mapeamento de "pergunta" para 422 e de falha para 502 ou 503 | Adotado nesta versão (seção 4.1 e 6.4) | Confirmar antes de publicar o contrato |
| P3 | Retenção do relato em `logs_execucoes` | O relato pode conter dado pessoal digitado por engano | (a) guardar o relato com prazo de retenção curto; (b) guardar só o HMAC do relato e manter o texto apenas nos casos de teste. **Adotado:** HMAC-SHA256 do relato, com chave secreta fora do banco. A `resposta_bruta` tem prazo de retenção (baseline aprovado: 30 dias) |
| P4 | Lista de chats do Telegram autorizados | Sem lista, qualquer pessoa com o link do bot pode registrar pedidos | **Adotado:** modo fail closed. Lista vazia ignora todos os chats. Os `chat_id` autorizados ficam na configuração segura (user-secrets ou variável de ambiente) |
| P5 | Limite de tamanho do relato | Proposto: 2.000 caracteres | **Adotado com cautela:** 2.000 caracteres como valor provisório, configurável em `Triagem:TamanhoMaximoRelato`. O maior relato do gabarito tem 101 caracteres, então o limite não restringe os casos de teste. Antes de fixar, validar com relatos fictícios de balcão e testar a fronteira do limite |

---

## 7. Atores

| Ator | Tipo | Papel no sistema |
| ---- | ---- | ---------------- |
| Atendente (Carla) | Primário | Registra o pedido pelo cliente REST ou pelo bot do Telegram e recebe ficha, pergunta ou aviso |
| Mecânico (Antônio) | Primário | Consulta as ordens por API (UC02) |
| Cliente (Rodrigo) | Secundário | Recebe a mensagem enviada pelo atendente. Não acessa o sistema |
| Desenvolvedor e avaliador (Eduardo) | Primário (manutenção e testes) | Mantém a API, o prompt, os testes e a avaliação |
| API do Gemini | Externo | Extrai, classifica e redige, devolvendo JSON |
| Telegram Bot API | Externo | Entrega as mensagens do chat ao webhook e recebe as respostas |
| PostgreSQL | Sistema de suporte | Armazena ordens, sintomas e registros de execução |

---

## 8. Trabalhos relacionados

Esta seção descreve categorias de solução que resolvem partes do mesmo problema. Não foi
feita uma revisão bibliográfica formal.

| Categoria | Exemplos de abordagem | Diferença deste projeto |
| --------- | --------------------- | ----------------------- |
| Sistemas de gestão para oficinas | Programas de ordem de serviço com cadastro, estoque e faturamento | Cobrem o fluxo financeiro. Este projeto foca na triagem do relato livre |
| Formulários de atendimento | Campos fixos preenchidos pelo atendente | Dependem de o atendente transcrever o relato. Este projeto aceita texto livre e estrutura |
| Extração de informação com LLM | Saída em JSON com esquema | Acrescenta validação em código e uma regra de segurança que não depende do modelo |
| Automação com n8n | Fluxos que chamam APIs de LLM e gravam resultados | Usado na versão 1.0. A versão 2.0 migra a orquestração para uma API própria |

**Lacuna que o projeto explora:** a validação determinística em código aplicada sobre a saída
de um LLM, medida contra um gabarito escrito antes da execução.

---

## 9. Conclusão

A especificação define uma versão enxuta e verificável. O atendente registra um relato em
texto livre pela API ou pelo bot e recebe a ficha estruturada, uma pergunta quando faltar
informação ou um aviso de urgência alta. O Gemini extrai e classifica; o código valida e
aplica as regras, com destaque para o piso que eleva a urgência de casos de risco.

Os requisitos RF01 a RF11 formam o escopo obrigatório, acrescidos de RF13 a RF15. O RF12
(lista de peças por agente) fica como versão 2. As pendências da seção 6.6 precisam de
decisão antes da fase de testes.

Os próximos documentos usam os identificadores desta especificação. O
[projeto de interface e contratos](04-Projeto-interface.md) define os endpoints, e a
[arquitetura](06-Arquitetura-solucao.md) define o modelo de dados e os componentes.

---

## 10. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.1 | 05/11/2026 | Confirma projeto único, nomes das suítes, regra de gravação da categoria Outro e manutenção da P1 fora de produção. O gabarito da seção 5.2 não foi alterado. |
| 2.0 | 04/11/2026 | Migração da especificação do fluxo n8n para a API .NET. |
