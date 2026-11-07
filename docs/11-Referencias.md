# 11 — Referências

- **Versão:** 2.1
- **Data da última mudança:** 07/11/2026
- **Substitui:** versão 2.0 (04/11/2026)

As fontes abaixo sustentam os contratos REST, a implementação C#, os testes, a persistência, a
integração com o Gemini e os controles de privacidade.

---

## 1. ASP.NET Core e contratos REST

| Tema | Fonte | Link | Aplicação no projeto |
| ---- | ----- | ---- | -------------------- |
| Minimal APIs | Microsoft Learn | [Visão geral de Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview) | `Program.cs` e `TriagemEndpoints` |
| ProblemDetails | IETF | [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html) | Respostas 400, 415, 502 e 503 |
| OpenAPI | OpenAPI Initiative | [OpenAPI 3.0.3](https://spec.openapis.org/oas/v3.0.3.html) | Contrato gerado em `/openapi/v1.json`; arquivo versionado ainda pendente |
| Configuração segura em desenvolvimento | Microsoft Learn | [Safe storage of app secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) | `Gemini:ApiKey`, `Privacidade:ChaveHmac` e connection string em user-secrets |

---

## 2. Gemini

| Tema | Fonte | Link | Aplicação no projeto |
| ---- | ----- | ---- | -------------------- |
| Método REST `generateContent` | Google AI for Developers | [Referência REST](https://ai.google.dev/api/rest/v1beta/models/generateContent) | Chamada tipada de `GeminiLlmClient` |
| Saída estruturada | Google AI for Developers | [Structured output](https://ai.google.dev/gemini-api/docs/structured-output) | `responseMimeType` e `responseJsonSchema` |
| Autenticação | Google AI for Developers | [API keys](https://ai.google.dev/gemini-api/docs/api-key) | Cabeçalho `x-goog-api-key`; valor fora do código |
| Limites de uso | Google AI for Developers | [Rate limits](https://ai.google.dev/gemini-api/docs/rate-limits) | Tratamento de 429 e `Retry-After` |

O esquema da API orienta o formato, mas a aplicação mantém `ValidadorSaidaLlm` como barreira
determinística para tipos, campos, categoria e urgência.

---

## 3. Persistência e contêineres

| Tema | Fonte | Link | Aplicação no projeto |
| ---- | ----- | ---- | -------------------- |
| Migrações EF Core | Microsoft Learn | [Managing migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/) | Migração `InitialCreate` e snapshot versionados |
| Provedor PostgreSQL | Npgsql | [EF Core provider](https://www.npgsql.org/efcore/) | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 |
| PostgreSQL 17 | PostgreSQL Global Development Group | [Documentação PostgreSQL 17](https://www.postgresql.org/docs/17/) | Banco dedicado `os_triage_api` |
| Docker Compose | Docker | [Compose documentation](https://docs.docker.com/compose/) | Orquestração da API e do PostgreSQL |
| Docker secrets no Compose | Docker | [Secrets in Compose](https://docs.docker.com/compose/how-tos/use-secrets/) | Chaves e senha montadas em `/run/secrets` |

Versões fixadas no projeto em 07/11/2026: EF Core Design 10.0.4, Npgsql EF Core 10.0.3 e imagem
`postgres:17`.

---

## 4. Testes

| Tema | Fonte | Link | Aplicação no projeto |
| ---- | ----- | ---- | -------------------- |
| xUnit.net v2 | xUnit.net | [Getting started](https://xunit.net/docs/getting-started/v2/getting-started) | `OsTriageApi.Tests.Unit`, xUnit 2.9.3 |
| Testes de ASP.NET Core | Microsoft Learn | [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests) | Referência para futura suíte HTTP; PostgreSQL real não usa Testcontainers nesta fase |

A suíte atual usa `ILlmClient` e `ITriagemRepository` simulados escritos à mão. A execução real com
PostgreSQL e Gemini é registrada separadamente e nunca é substituída por resultado simulado.

---

## 5. Privacidade e segurança

| Tema | Fonte | Link | Aplicação no projeto |
| ---- | ----- | ---- | -------------------- |
| HMAC | IETF | [RFC 2104](https://www.rfc-editor.org/rfc/rfc2104) | HMAC-SHA256 do relato |
| LGPD atualizada | Câmara dos Deputados | [Lei nº 13.709/2018](https://www2.camara.leg.br/legin/fed/lei/2018/lei-13709-14-agosto-2018-787077-normaatualizada-pl.html) | Pseudonimização, retenção e segurança |
| Guia sobre anonimização | ANPD | [Nota técnica da consulta pública](https://www.gov.br/anpd/pt-br/acesso-a-informacao/participacao-social/outras-acoes/ts-01-2024-nota-tenica-6-de-abertura.pdf/@@display-file/file) | Referência técnica; não tratada como norma final |

O HMAC é pseudonimização, não anonimização. A chave fica separada do banco, e somente relatos
fictícios devem ser usados até uma revisão formal de privacidade para dados reais.

---

## 6. Pendências de conferência

- Gerar e revisar `docs/openapi.yaml` a partir do contrato real.
- Confirmar o modelo Gemini e a cota disponíveis na conta usada para a execução.
- Executar e registrar PostgreSQL 17, migração e CT-01/CT-13 no ambiente local.
- Rever a fonte final da ANPD antes de uso com dados reais.

---

## 7. Histórico de versões

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.1 | 07/11/2026 | Remove referências técnicas legadas e atualiza fontes para Minimal APIs, Gemini REST, EF Core, Npgsql, xUnit e Docker secrets. |
| 2.0 | 04/11/2026 | Organiza as referências iniciais da API .NET. |
