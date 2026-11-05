# 12 — Manual de instalação e execução

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (instalação do n8n em Docker)

Este manual leva da máquina vazia até a API respondendo, com o banco migrado, os testes
executados e, se desejado, o canal do Telegram ligado. Os comandos são para o PowerShell do
Windows 11. Os equivalentes em bash estão ao lado, quando diferem.

---

## 1. Pré-requisitos

| Requisito | Detalhe | Verificação |
| --------- | ------- | ----------- |
| Windows | Windows 11 23H2 (build 22631) ou superior | `winver` |
| Memória | 8 GB de RAM, no mínimo. Feche programas pesados durante a rodada de avaliação | — |
| Virtualização | Ativada na BIOS/UEFI (Intel VT-x ou AMD-V) | Gerenciador de Tarefas, aba Desempenho |
| WSL 2 | Versão 2.1.5 ou superior | `wsl --version` |
| Docker Desktop | Versão atual, com o backend WSL 2 | `docker --version` |
| .NET SDK | Versão fixada em `global.json` do repositório | `dotnet --version` |
| Git | Para clonar o repositório | `git --version` |
| Conta Google | Para criar a chave do Gemini no Google AI Studio | — |
| Telegram (opcional) | Conta e token de bot, criados pelo BotFather. Só para o canal do chat | — |

Se o `wsl --version` não mostrar a versão, instale ou atualize o WSL como administrador:

```powershell
wsl --install
```

Reinicie o computador se o instalador pedir.

---

## 2. Instalar o .NET SDK

1. Baixe o SDK em [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download), na versão
   indicada no arquivo `global.json` do repositório.
2. Instale com as opções padrão.
3. Abra um terminal novo e confirme:

```powershell
dotnet --version
```

Se a versão mostrada for diferente da fixada no `global.json`, o SDK correto será usado
automaticamente quando o terminal estiver dentro da pasta do repositório, desde que o SDK esteja
instalado. Se houver erro de versão, instale a indicada.

---

## 3. Instalar o Docker Desktop

1. Baixe o Docker Desktop em [docs.docker.com, instalação para Windows](https://docs.docker.com/desktop/setup/install/windows-install/).
2. Execute o instalador. Escolha o modo **por usuário**, que não exige administrador.
3. Na tela de configuração, selecione o backend **WSL 2**.
4. Abra o Docker Desktop e aguarde o ícone indicar que está em execução.
5. Confirme:

```powershell
docker --version
docker compose version
```

Se o Docker Desktop não iniciar, consulte a seção 13.

---

## 4. Clonar o repositório

```powershell
git clone <URL do repositório os-triage-api>
cd os-triage-api
```

Substitua `<URL do repositório os-triage-api>` pelo endereço do repositório do projeto.

---

## 5. Configurar o ambiente

Copie o modelo de configuração:

```powershell
copy .env.example .env
```

O `.env` tem apenas uma variável de segredo:

| Variável | Função | Como preencher |
| -------- | ------ | -------------- |
| `POSTGRES_PASSWORD` | Senha do usuário do PostgreSQL usado pela API | Uma senha forte, gerada por você. Use a mesma no passo 7 |

O `.env` não vai para o GitHub. A chave do Gemini, a chave HMAC e o token do Telegram **não** ficam
no `.env`: eles vão para os user-secrets, no passo 7.

---

## 6. Subir o PostgreSQL

```powershell
docker compose up -d postgres
docker compose ps
```

Aguarde o estado `healthy`. Para ver os logs:

```powershell
docker compose logs -f postgres
```

O banco criado é `os_triage_api`, com o usuário `os_triage_app`. Ele é dedicado à API, e não
compartilha tabelas com outros projetos (ver [arquitetura](06-Arquitetura-solucao.md), seção 4.8).

---

## 7. Configurar os segredos

Os segredos ficam nos **user-secrets** do projeto da API, fora da pasta do repositório.

```powershell
cd src\OsTriage.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=os_triage_api;Username=os_triage_app;Password=<mesma senha do .env>"
dotnet user-secrets set "Gemini:ApiKey" "<chave do Google AI Studio>"
dotnet user-secrets set "Privacidade:ChaveHmac" "<chave aleatória de pelo menos 32 bytes>"
dotnet user-secrets set "Telegram:SecretToken" "<valor aleatório>"
cd ..\..
```

Para gerar a chave HMAC e o segredo do webhook, use um gerador criptográfico. O `openssl` vem com o
Git for Windows:

```powershell
openssl rand -base64 32
```

Em bash, o mesmo comando funciona sem alteração.

**Atenção à chave HMAC.** Ela calcula o `relato_hash` de cada registro. Se for trocada, os registros
antigos deixam de ser comparáveis com os novos. Guarde uma cópia segura e não a altere sem registrar
a decisão.

**Chats do Telegram (somente se usar o canal).** A lista de chats autorizados começa vazia, e uma
lista vazia significa **nenhum chat autorizado** (modo fail closed). Para autorizar um chat:

```powershell
cd src\OsTriage.Api
dotnet user-secrets set "Telegram:ChatsPermitidos:0" "<chat_id autorizado>"
cd ..\..
```

---

## 8. Aplicar as migrações

As ferramentas do projeto, incluindo `dotnet-ef`, são restauradas a partir do manifesto:

```powershell
dotnet tool restore
dotnet ef database update --project src\OsTriage.Infrastructure --startup-project src\OsTriage.Api
```

Confirme que as tabelas existem:

```powershell
docker compose exec postgres psql -U os_triage_app -d os_triage_api -c "\dt"
```

Devem aparecer `ordens_servico`, `sintomas` e `logs_execucoes`, além da tabela de histórico do EF
Core (`__EFMigrationsHistory`).

---

## 9. Executar a API

```powershell
dotnet run --project src\OsTriage.Api
```

A API responde em `http://localhost:5000`, somente nesta máquina. Para conferir o contrato:

- `http://localhost:5000/openapi/v1.json`: documento OpenAPI gerado pela própria API.
- O arquivo versionado `docs/openapi.yaml` é a referência; o teste de contrato garante que os dois
  coincidem.

Para parar a API, pressione `Ctrl+C` no terminal.

---

## 10. Primeiro teste

Envie um relato fictício:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/triagem `
  -ContentType "application/json" `
  -Body '{"relato":"Honda CB 300 2017. O freio dianteiro está mole."}'
```

Resultado esperado: resposta com `resultado = gravada`, `urgenciaFinal = alta` e
`avisoUrgencia = true`. A mensagem ao cliente tem até 60 palavras.

Confira a linha gravada:

```powershell
docker compose exec postgres psql -U os_triage_app -d os_triage_api -c "SELECT categoria, urgencia_llm, urgencia_final FROM ordens_servico;"
```

Repetir o mesmo envio com a mesma `Idempotency-Key` não cria outra linha:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/triagem `
  -Headers @{ "Idempotency-Key" = "teste-001" } `
  -ContentType "application/json" `
  -Body '{"relato":"Honda CB 300 2017. O freio dianteiro está mole."}'
```

---

## 11. Executar os testes

Os testes unitários e de contrato não precisam de Docker. Os de integração precisam do Docker
ativo, porque sobem um PostgreSQL descartável.

```powershell
dotnet test
```

Para rodar só a suíte unitária:

```powershell
dotnet test tests\OsTriage.Tests.Unit
```

### 11.1 Avaliação com o Gemini

A avaliação chama o Gemini real, usa o nível gratuito e faz 72 chamadas por rodada (24 casos, três
execuções cada). Por isso ela só roda quando habilitada explicitamente:

```powershell
$env:AVALIACAO_HABILITADA = "true"
dotnet test tests\OsTriage.Tests.Avaliacao
Remove-Item Env:AVALIACAO_HABILITADA
```

Os resultados vão para `data/`, que não é versionada. Antes de rodar, confira os limites no
[Google AI Studio](https://aistudio.google.com/). Se a cota se esgotar, a suíte para e registra os
casos pendentes.

---

## 12. Canal do Telegram (opcional)

O webhook do Telegram exige um endereço HTTPS público. Em desenvolvimento, use um túnel para o
`localhost:5000`. A ferramenta de túnel fica a critério de quem executa, desde que forneça HTTPS.

1. Inicie o túnel e anote a URL HTTPS gerada.
2. Registre o webhook na Bot API, com o segredo configurado no passo 7:

```powershell
curl.exe -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" `
  -d "url=<URL_HTTPS>/api/telegram/webhook" `
  -d "secret_token=<mesmo valor de Telegram:SecretToken>" `
  -d "allowed_updates=[`"message`"]"
```

3. Envie uma mensagem ao bot a partir de um chat autorizado. Mensagens de chats fora da lista não
   recebem resposta.

O token do bot nunca vai para o repositório. Se ele for exposto, revogue-o pelo BotFather e
registre o webhook de novo.

---

## 13. Comandos úteis

| Ação | Comando |
| ---- | ------- |
| Ver se o PostgreSQL está saudável | `docker compose ps` |
| Ver os logs do PostgreSQL | `docker compose logs -f postgres` |
| Compilar a solução | `dotnet build` |
| Listar as migrações | `dotnet ef migrations list --project src\OsTriage.Infrastructure --startup-project src\OsTriage.Api` |
| Parar o PostgreSQL, mantendo os dados | `docker compose down` |

**Atenção:** não use `docker compose down -v`. A opção `-v` apaga o volume `os_triage_pgdata`, com
todas as ordens e os registros de execução.

---

## 14. Arquivo de referência: `docker-compose.yml`

O arquivo abaixo é o que está no repositório. Os valores sensíveis vêm do `.env`.

```yaml
# PostgreSQL do projeto os-triage-api.
# Lê POSTGRES_PASSWORD do arquivo .env desta pasta.
# Subir:  docker compose up -d postgres     Parar:  docker compose down
# NUNCA use "docker compose down -v": ele apaga o volume os_triage_pgdata,
# que guarda todas as ordens e os registros de execução.

services:
  postgres:
    image: postgres:17
    container_name: os_triage_postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: os_triage_api
      POSTGRES_USER: os_triage_app
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?Defina POSTGRES_PASSWORD no arquivo .env}
      POSTGRES_INITDB_ARGS: "--encoding=UTF8"
    ports:
      # Publicado somente em localhost. Não fica acessível pela rede.
      - "127.0.0.1:5432:5432"
    volumes:
      - os_triage_pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U os_triage_app -d os_triage_api"]
      interval: 5s
      timeout: 3s
      retries: 10

volumes:
  os_triage_pgdata:
    name: os_triage_pgdata
```

**Observações sobre o arquivo:**

- A imagem `postgres:17` é a versão principal fixada. A versão menor exata pode ser fixada depois da
  primeira subida, como o n8n era fixado na versão 1.0.
- O usuário da aplicação tem privilégios apenas sobre o próprio banco. Não é superusuário.
- O volume `os_triage_pgdata` sobrevive a `docker compose down`.

---

## 15. Solução de problemas

| Sintoma | Causa provável | O que fazer |
| ------- | -------------- | ----------- |
| `docker` não é reconhecido | Docker Desktop não foi iniciado | Abra o Docker Desktop e aguarde o ícone |
| Docker Desktop não inicia | WSL 2 desatualizado ou virtualização desligada | Atualize o WSL (`wsl --install`) e ative a virtualização na BIOS/UEFI |
| `password authentication failed` | Senha diferente entre `.env` e user-secrets | Use a mesma senha nos dois lugares e reinicie o contêiner |
| Porta 5432 já em uso | Outro PostgreSQL local | Pare o outro serviço, ou mude a porta publicada no compose e na connection string |
| `dotnet user-secrets` não encontra o projeto | Comando rodado fora de `src\OsTriage.Api` | Entre na pasta do projeto, ou use `--project` |
| Migração falha com `relation already exists` | Banco errado: outro projeto usa o mesmo banco | Confirme `Database=os_triage_api` na connection string. A API não deve dividir banco com outro projeto |
| `503` com `codigo = ia_indisponivel` | Chave do Gemini inválida ou cota esgotada | Confira a chave em user-secrets; se for cota, espere o reset diário (meia-noite do Pacífico) |
| `502` com `codigo = saida_llm_invalida` | O modelo não cumpriu o esquema após as tentativas | Consulte `logs_execucoes` para ver a resposta bruta; isso é esperado em parte dos casos e entra na métrica de validade do JSON |
| `401` no webhook do Telegram | `X-Telegram-Bot-Api-Secret-Token` diferente do configurado | Registre o webhook de novo com o mesmo valor de `Telegram:SecretToken` |
| Telegram não responde | Chat fora da lista autorizada (inclusive lista vazia) | Confirme `Telegram:ChatsPermitidos` em user-secrets |
| `400` ao chamar a API | JSON inválido, campo desconhecido ou `Content-Type` ausente | Envie `Content-Type: application/json` e somente o campo `relato` |
| Testes de integração ignorados | Docker não está ativo | Abra o Docker Desktop e rode `dotnet test` de novo |
| Limite (429) do Gemini | Limite do nível gratuito atingido | Espere o próximo minuto ou o reset diário; rode a avaliação em lotes com pausa |
