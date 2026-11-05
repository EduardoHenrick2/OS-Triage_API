# 03 — Metodologia

- **Versão:** 2.1
- **Data da última mudança:** 05/11/2026
- **Substitui:** versão 1.0 (30/10/2026)

---

## 1. Abordagem

O projeto segue fases sequenciais, e cada fase só termina quando o critério de saída é cumprido.
Dentro da fase de testes, o prompt passa por ciclos curtos de medição e ajuste, cada um com uma
única mudança.

Quatro princípios orientam as decisões:

- **Entregar a versão 1 antes de pensar na versão 2.** A versão 2 do prompt e a lista de peças
  por agente (RF12) só entram se houver folga após a fase de testes.
- **Medir antes de melhorar.** Nenhuma mudança no prompt ou na lista de risco entra sem a rodada
  de 24 casos que a comprova.
- **Separar IA e código.** Mudanças de regra de negócio são feitas no código do domínio, não no
  prompt. O banco reforça as regras essenciais com restrições.
- **Contrato e gabarito antes da implementação.** O contrato OpenAPI e o gabarito de 24 casos
  são congelados antes da rodada de medição. Os testes são escritos a partir deles, não o
  contrário.

---

## 2. Cronograma

As datas são uma **proposta a validar** a partir de 04/11/2026. O cronograma original (versão
1.0, de 30/10/2026) previa a fase de ambiente em n8n, já concluída em parte. A migração para a
API .NET não estava no plano e consome parte da folga. Por isso, o cronograma foi replanejado.

| Fase | Período (proposta) | Entrega | Critério de saída |
| ---- | ------------------ | ------- | ----------------- |
| 1. Ambiente .NET | 04/11 a 05/11 | Projeto único `OsTriageApi`, suítes separadas, PostgreSQL em Docker, primeira migração e chamada de teste ao Gemini | `dotnet test` roda sem erro; uma chamada ao Gemini volta com resposta; uma ordem de teste é gravada |
| 2. Núcleo de domínio | 05/11 a 07/11 | Categorias, detector de risco, confirmação de dados, validação de saída e serviço de triagem com LLM simulado (RF01 a RF05, RF11) | Testes unitários do núcleo passam; o serviço responde a 5 casos de teste sem erro |
| 3. API, persistência e canais | 08/11 a 09/11 | Endpoints, EF Core, idempotência, logs de execução, ProblemDetails, webhook do Telegram em fail closed (RF06, RF07, RF09, RF10, RF13, RF14) | Testes de integração passam; ficha gravada uma única vez; falha de API tratada com 503 |
| 4. Testes e avaliação | 10/11 a 12/11 | 24 casos, métricas da v1 e da v2 do prompt, relatório (RF08) | Metas de precisão atingidas, ou data-limite de 12/11/2026 |
| 5. Entrega | 13/11 a 16/11 | README, vídeo de até 3 minutos, revisão da documentação e divulgação (currículo e LinkedIn) | Definição de pronto cumprida (seção 8 do [plano de testes](07-Plano-testes-software.md)) |

**Status da fase 1 em 05/11/2026:** em andamento. O projeto compila sem erros ou avisos e a suíte
`OsTriageApi.Tests.Unit` passa, mas PostgreSQL, migração e chamada ao Gemini permanecem fora desta
etapa. A fase só será marcada como concluída quando todos os critérios da tabela forem executados.

**Prazos externos:** a entrega do projeto é **16/11/2026**. As inscrições do programa vão até
**17/11/2026**, segundo o Seja Trainee. Essa data precisa ser confirmada na página oficial da vaga
antes de qualquer entrega.

### 2.1 Detalhamento da fase 1

1. Instalar o .NET SDK na versão fixada em `global.json` e confirmar com `dotnet --version`.
2. Criar o projeto único `src/OsTriageApi`, organizado por pastas de camada, e as três suítes
   `OsTriageApi.Tests.Unit`, `OsTriageApi.Tests.Integration` e `OsTriageApi.Tests.Avaliacao`.
3. Subir o PostgreSQL com `docker compose up -d postgres` e aplicar a primeira migração.
4. Criar a chave no Google AI Studio e cadastrá-la em user-secrets.
5. Criar o repositório `os-triage-api` com `.gitignore` para segredos, `.env` e a pasta `data/`.
6. Fixar as versões de pacotes no `csproj` e de ferramentas em `.config/dotnet-tools.json`.

### 2.2 Ciclo de ajuste do prompt (fase 4)

1. Rodar os 24 casos com o prompt v1 e salvar todas as respostas.
2. Agrupar os erros por tipo: categoria, urgência, campo inventado ou formato.
3. Mudar uma coisa por vez no prompt: instrução, definição de categoria ou exemplos. O resultado é
   o prompt v2, versionado em `prompts/`.
4. Rodar os 24 casos de novo, sem alterar o gabarito.
5. Comparar v1 e v2 nas métricas e manter a versão melhor. Os resultados vão para o registro de
   testes.

O mesmo ciclo vale para a lista de palavras de risco (proposta da seção 6.5 da
[especificação](02-Especificacao.md)): uma mudança por vez, com teste unitário e rodada da
avaliação.

### 2.3 Ordem de corte se o prazo apertar

Se a folga acabar, cortar nesta ordem:

1. Versão 2 do prompt. A v1 já cumpre o objetivo de medição; a v2 mostra a evolução.
2. Canal do Telegram (RF14). Pode ser entregue com o webhook coberto por testes de integração,
   sem túnel público, e isso deve ser declarado no relatório.
3. Índices e a tela de consulta de ordens (RF13), desde que a consulta por API continue funcional.

**Não cortar:** RF01 a RF05 e RF11, a validação de saída, a regra de risco, o gabarito congelado,
as métricas da v1 e os testes de integração do contrato HTTP (200, 400, 422, 502 e 503).

---

## 3. Justificativa das ferramentas

O projeto precisa custar R$ 0 (RNF01). Cada ferramenta abaixo foi escolhida pelo custo e pelo que
ela entrega ao projeto. A tabela também registra as limitações conhecidas, para que elas entrem
no planejamento.

| Ferramenta | Por que foi escolhida | Alternativas descartadas | Custo e limite conhecido |
| ---------- | --------------------- | ------------------------ | ------------------------ |
| .NET SDK (LTS) | Runtime e SDK da API, dos testes e das migrações | .NET em versão sem suporte de longo prazo | Gratuito e de código aberto |
| ASP.NET Core Minimal APIs | Endpoints explícitos, com DTOs e ProblemDetails; contrato fácil de expor como OpenAPI | Controllers (MVC): válidos, mas com mais estrutura do que a API precisa | Gratuito |
| EF Core com Npgsql | Mapeamento, migrações versionadas e consultas parametrizadas | Dapper (mais controle de SQL, mais código manual de migração); SQL puro | Gratuito |
| PostgreSQL 17 em Docker | Restrições de integridade, arrays e colunas geradas; banco dedicado ao projeto | SQLite (sem tipos fortes); planilha ou CSV (sem integridade) | Gratuito. Exige Docker e algum espaço em disco |
| Docker Desktop com WSL 2 | Isola o PostgreSQL e fixa a versão pelo `docker-compose.yml` | Instalação direta do PostgreSQL no Windows | Gratuito para uso pessoal e educação. Exige WSL 2 e 8 GB de RAM |
| Gemini API (Google AI Studio) | Extração, classificação e redação com saída em JSON | Modelos pagos, que aumentariam o custo acima de R$ 0 | Nível gratuito com limites de RPM, TPM e RPD por projeto. O contador diário zera à meia-noite do horário do Pacífico |
| xUnit | Padrão de testes do ecossistema .NET, com execução por `dotnet test` | NUnit e MSTest (também válidos) | Gratuito |
| Testcontainers para .NET | Sobe PostgreSQL descartável para os testes de integração | Banco fixo na máquina do desenvolvedor, que contamina os testes | Gratuito. Exige Docker ativo |
| Telegram Bot API | Canal de atendimento por chat | Canal próprio de mensagens, fora do escopo | Gratuito. O webhook exige endereço HTTPS público |
| Git e GitHub | Versiona código, migrações, prompts, gabarito e relatórios | Armazenamento só local, sem histórico público | Gratuito |
| Gravador de tela | Produz o vídeo de demonstração de até 3 minutos | Capturas de tela estáticas, que não mostram o fluxo | Gratuito |

**Ponto de atenção:** o Docker Desktop consome memória. Antes da fase 4, confirme a memória livre
da máquina, porque a rodada de 24 casos com 3 execuções cada, mais os testes de integração, gera
carga contínua.

---

## 4. Riscos de cronograma

| Risco | Mitigação |
| ----- | --------- |
| Migração para .NET consome o prazo de testes | Fase 1 curta; corte pela ordem da seção 2.3 |
| Pouco tempo por causa de aulas e outras entregas | Priorizar RF01 a RF05, RF11 e os testes; a versão 2 do prompt só entra com folga |
| Limite do nível gratuito atrasa as rodadas | Lotes com pausa; salvar respostas; conferir limites no AI Studio |
| Docker ou PostgreSQL não sobem | Plano B: PostgreSQL instalado localmente, com a mesma versão, e a mesma connection string |
| Túnel HTTPS do Telegram instável | Cobrir o webhook por testes de integração, que não dependem de túnel |
| Migração aplicada em banco errado ou com nome de tabela em conflito | Banco dedicado `os_triage_api` e role própria (arquitetura, seção 4.8) |

Os riscos técnicos completos estão na [arquitetura](06-Arquitetura-solucao.md), seção 9.

---

## 9. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.1 | 05/11/2026 | Adota um projeto de aplicação com pastas por responsabilidade e registra o estado real da fase 1. |
| 2.0 | 04/11/2026 | Replanejamento da implementação para .NET. |
