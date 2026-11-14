# Guia operacional para agentes

Este arquivo orienta qualquer agente que edite o código-fonte do projeto Triagem de Ordens de
Serviço. Antes de alterar o código, leia os documentos de contexto e confirme o estado atual de
`src/OsTriageApi` e `tests/`. As regras abaixo são o contrato operacional vigente; quando houver
dúvida, a especificação e a arquitetura prevalecem sobre uma conveniência de implementação.

## 1. Arquitetura e regras vigentes

### 1.1 Stack e limites arquiteturais

- A aplicação é uma API REST em **.NET 10**, construída com **Minimal APIs**.
- A persistência usa **PostgreSQL com Entity Framework Core** e migrações versionadas.
- O projeto mantém `Nullable` habilitado.
- O JSON público usa nomes em `camelCase`.
- A desserialização rejeita membros desconhecidos com `JsonUnmappedMemberHandling.Disallow`.
- O Gemini interpreta o relato; o código C# valida, aplica as regras de negócio e decide o que
  pode ser gravado; o banco reforça as invariantes por restrições.
- A API deve escutar localmente em `127.0.0.1` conforme o manual de instalação.

Referências normativas: [docs/01-Contexto.md](../docs/01-Contexto.md) e
[docs/06-Arquitetura-solucao.md](../docs/06-Arquitetura-solucao.md).

### 1.2 Pipeline vigente de `POST /api/triagem`

Preserve o fluxo abaixo e suas responsabilidades. Novos comportamentos devem ser adicionados no
componente correto, sem transferir regra de negócio para o endpoint ou para o provedor de LLM:

```text
CriarRelatoRequest
  → RelatoValidador
  → TriagemService
  → RegrasUrgencia.BuscarExpressao
  → GeminiLlmClient (ILlmClient)
  → ValidadorSaidaLlm
  → DecisorTriagem
  → EfTriagemRepository
  → LimitadorPalavras (60 palavras)
```

Na implementação atual, `TriagemService` também registra cada tentativa em `logs_execucoes`,
faz idempotência por `Idempotency-Key`, gera a mensagem após a gravação e devolve `422`, `502` ou
`503` sem gravar uma ficha inválida. O `LimitadorPalavras` é aplicado à etapa de redação da
mensagem ao cliente.

### 1.3 Regras invioláveis

As regras desta seção vêm de [docs/02-Especificacao.md](../docs/02-Especificacao.md):

- Existem exatamente oito categorias canônicas, centralizadas em `CategoriasCanonicas`.
- As urgências válidas são `baixa`, `media` e `alta`, nessa ordem (`baixa < media < alta`),
  centralizadas em `Urgencias`.
- Termos de risco vigentes detectados por `RegrasUrgencia.BuscarExpressao` impõem piso de
  urgência `alta`.
- O banco deve continuar reforçando o piso com `ck_ordens_piso_urgencia` e a relação entre
  risco e urgência com `ck_ordens_risco`.
- A **Proposta P1**, descrita na seção 6.6 de `docs/02-Especificacao.md`, permanece
  **não vigente**. Ela só poderá ser ativada depois de testes unitários específicos e uma rodada
  de avaliação sem regressão no gabarito congelado.
- Não introduza categorias, urgências, sinônimos de risco ou mudanças de precedência sem atualizar
  a especificação, os testes e as restrições correspondentes.

### 1.4 Segurança, privacidade e LGPD

Conforme [docs/06-Arquitetura-solucao.md](../docs/06-Arquitetura-solucao.md) e
[docs/12-Manual-instalacao.md](../docs/12-Manual-instalacao.md):

- O hash do relato deve usar **HMAC-SHA256**.
- `Privacidade:ChaveHmac` é obrigatória e deve ter pelo menos 32 caracteres.
- Segredos só podem vir de Docker secrets montados em `/run/secrets` ou de `user-secrets`.
- Nunca grave chaves, tokens, senhas, dados reais de clientes ou valores de secrets em arquivos
  versionados.
- A retenção deve preservar o hash e os metadados necessários para auditoria, apagando somente a
  resposta bruta do LLM quando o prazo for atingido.

## 2. Estado atual e backlog ordenado

O estado encontrado no código contém o projeto `src/OsTriageApi`, migração inicial, endpoint de
triagem e suíte `tests/OsTriageApi.Tests.Unit`. Não foram encontrados ainda os endpoints de
consulta, suíte de integração, suíte de avaliação, arquivo de solução, manifesto de ferramentas,
prompt externo, webhook do Telegram ou `docs/openapi.yaml` sincronizado.

Implemente os itens nesta ordem. Cada item deve manter a referência documental indicada e incluir
testes adequados antes de ser considerado concluído.

### Bloco 1 — Core e Governança

Este bloco é **não cortável**. Referência de prioridade: [docs/03-Metodologia.md](../docs/03-Metodologia.md),
seção 2.

1. Implementar `GET /api/ordens-servico` e `GET /api/ordens-servico/{id}`, respeitando os
   contratos de [docs/04-Projeto-interface.md](../docs/04-Projeto-interface.md), seção 6, e as
   regras de [docs/02-Especificacao.md](../docs/02-Especificacao.md).
2. Extrair os prompts atualmente embutidos em `GeminiLlmClient` para `prompts/v1.md`. O arquivo
   deve ser carregado pela configuração `Gemini:VersaoPrompt`; a aplicação deve falhar no startup
   se a versão configurada estiver ausente. Referência: [docs/06-Arquitetura-solucao.md](../docs/06-Arquitetura-solucao.md).
3. Criar rotina idempotente de retenção LGPD usando `Privacidade:RetencaoRespostaBrutaDias`, com
   padrão `30`, zerando somente `resposta_bruta` em `logs_execucoes`. Referência: seção 4.9 de
   [docs/06-Arquitetura-solucao.md](../docs/06-Arquitetura-solucao.md).
4. Publicar `docs/openapi.yaml` sincronizado com `MapOpenApi`, conforme
   [docs/04-Projeto-interface.md](../docs/04-Projeto-interface.md), e criar `Directory.Build.props`
   aplicando `TreatWarningsAsErrors` também a `src/OsTriageApi`.

### Bloco 2 — Testes, gabarito e ferramentas

Referência principal: [docs/07-Plano-testes-software.md](../docs/07-Plano-testes-software.md).

1. Criar o arquivo de solução (`.sln` ou `.slnx`), `.config/dotnet-tools.json` com `dotnet-ef` e
   completar `.gitattributes` para `*.cs`, `*.csproj`, `*.json` e `*.md` usando `LF`, conforme
   [docs/07-Plano-testes-software.md](../docs/07-Plano-testes-software.md).
2. Criar `tests/OsTriageApi.Tests.Integration` usando `WebApplicationFactory`, PostgreSQL
   Testcontainers e `ILlmClient` simulado. Cobrir `CN13`, `CN16`, `CN17`, `CN20`, `CN21`, `CN23`,
   `CN24`, `TE04` e `TE06`, conforme [docs/07-Plano-testes-software.md](../docs/07-Plano-testes-software.md).
3. Exportar o gabarito de 24 casos das seções 5.2 e 5.3 de
   [docs/07-Plano-testes-software.md](../docs/07-Plano-testes-software.md) para
   `tests/casos_teste.csv`.
4. Registrar o commit real de congelamento do gabarito, substituindo a referência inexistente
   `dcd385f`. O gabarito congelado não pode ser alterado para fazer um teste passar.
5. Criar `tests/OsTriageApi.Tests.Avaliacao` com opt-in explícito, calculando as métricas da seção
   6 de [docs/07-Plano-testes-software.md](../docs/07-Plano-testes-software.md).

### Bloco 3 — Escopo secundário

Este bloco é **cortável se o prazo apertar**. Referência de prioridade: seção 2.3 de
[docs/03-Metodologia.md](../docs/03-Metodologia.md).

1. Implementar `POST /api/telegram/webhook` em modo *fail-closed*, conforme a seção 5.4 de
   [docs/04-Projeto-interface.md](../docs/04-Projeto-interface.md) e
   [docs/06-Arquitetura-solucao.md](../docs/06-Arquitetura-solucao.md): validar
   `X-Telegram-Bot-Api-Secret-Token`, manter a lista de chats permitidos vazia bloqueando tudo,
   garantir idempotência por `telegram:{update_id}` e descartar o campo `from`.
2. Avaliar isoladamente a Proposta P1 antes de qualquer ativação em `RegrasUrgencia`, usando os
   testes unitários e a avaliação sem regressão exigidos na seção 6.6 de
   [docs/02-Especificacao.md](../docs/02-Especificacao.md).

## 3. Checklist obrigatório antes de concluir uma alteração

- [ ] `dotnet build` executado na raiz.
- [ ] `dotnet test` executado na raiz; testes que exigem Docker foram tratados conforme o manual.
- [ ] `docker compose config --quiet` executado na raiz.
- [ ] Contratos, regras, migrações e documentação afetados foram atualizados juntos.
- [ ] O gabarito congelado não foi alterado para mascarar regressões.
- [ ] Nenhuma referência ao n8n foi reintroduzida nos documentos ativos ou no caminho de execução.
- [ ] Nenhum segredo, token, chave, dado real ou resposta bruta indevida foi adicionado ao
  repositório.
- [ ] Os testes de risco, urgência, categorias, idempotência e validação da saída do LLM continuam
  cobrindo as invariantes do domínio.

Referência operacional dos comandos e do ambiente: [docs/12-Manual-instalacao.md](../docs/12-Manual-instalacao.md).
