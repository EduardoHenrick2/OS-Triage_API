# 11 — Referências

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (30/10/2026)

Esta lista cobre a plataforma .NET e a API, os testes, a LGPD e a arquitetura. Cada item diz se foi
lido na íntegra, se apenas localizado ou se ainda precisa ser conferido. Um link só entra como
"conferido" depois de aberto e lido.

---

## 1. Plataforma e API

| Tema | Fonte | Link | Usado em | Situação em 04/11/2026 |
| ---- | ----- | ---- | -------- | ---------------------- |
| Minimal APIs no ASP.NET Core | Microsoft Learn | [learn.microsoft.com, Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview) | [04](04-Projeto-interface.md), [06](06-Arquitetura-solucao.md) | Localizado; conteúdo não lido na íntegra |
| Política de suporte do .NET (LTS) | Microsoft | [dotnet.microsoft.com, política de suporte](https://dotnet.microsoft.com/platform/support-policy/dotnet-core) | [03](03-Metodologia.md), [12](12-Manual-instalacao.md) | Localizado; conferir a janela de suporte do .NET 10 na página |
| Problem Details para APIs HTTP (RFC 9457) | IETF | [rfc-editor.org, RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html) | [04](04-Projeto-interface.md), [06](06-Arquitetura-solucao.md) | Localizado; conteúdo não lido na íntegra |
| OpenAPI 3.0.3 | OpenAPI Initiative | [spec.openapis.org, OpenAPI 3.0.3](https://spec.openapis.org/oas/v3.0.3.html) | `docs/openapi.yaml`, [04](04-Projeto-interface.md) | Localizado; conteúdo não lido na íntegra |
| HMAC (RFC 2104) | IETF | [rfc-editor.org, RFC 2104](https://www.rfc-editor.org/rfc/rfc2104) | [06](06-Arquitetura-solucao.md) (`relato_hash`), [02](02-Especificacao.md) (P3) | Localizado; conteúdo não lido na íntegra |

---

## 2. Testes

| Tema | Fonte | Link | Usado em | Situação em 04/11/2026 |
| ---- | ----- | ---- | -------- | ---------------------- |
| xUnit.net, guia de início (v2) | xUnit.net | [xunit.net, guia de início v2](https://xunit.net/docs/getting-started/v2/getting-started) | [07](07-Plano-testes-software.md), [12](12-Manual-instalacao.md) | Localizado; a versão 2.9.2 é a fixada no projeto |
| Testcontainers para .NET, módulos (PostgreSQL) | Testcontainers | [dotnet.testcontainers.org, módulos](https://dotnet.testcontainers.org/modules/) | [07](07-Plano-testes-software.md), [06](06-Arquitetura-solucao.md) | Localizado; conferir o módulo PostgreSQL na página |

---

## 3. LGPD e privacidade

| Tema | Fonte | Link | Usado em | Situação em 04/11/2026 |
| ---- | ----- | ---- | -------- | ---------------------- |
| Lei nº 13.709/2018 (LGPD), texto atualizado | Câmara dos Deputados | [camara.leg.br, LGPD atualizada](https://www2.camara.leg.br/legin/fed/lei/2018/lei-13709-14-agosto-2018-787077-normaatualizada-pl.html) | [02](02-Especificacao.md), [06](06-Arquitetura-solucao.md) | Trechos lidos: art. 5º, III e XI; art. 13, § 4º (início da definição); art. 16; art. 46; art. 48 |
| Texto oficial da LGPD | Presidência da República | [planalto.gov.br, Lei 13.709](https://planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm) | Conferência | Acesso bloqueado em 04/11/2026. Usar o texto da Câmara até confirmar o oficial |
| Guia de anonimização e pseudonimização | ANPD | [gov.br/anpd, nota técnica de abertura da consulta (2024)](https://www.gov.br/anpd/pt-br/acesso-a-informacao/participacao-social/outras-acoes/ts-01-2024-nota-tenica-6-de-abertura.pdf/@@display-file/file) | [06](06-Arquitetura-solucao.md), seção de LGPD | Documento de consulta pública, não norma final. Verificar se houve versão final antes da entrega |

### 3.1 Trechos da LGPD usados no projeto

- **Art. 5º, III (dado anonimizado):** dado relativo a titular que não possa ser identificado, considerando a utilização de meios técnicos razoáveis.
- **Art. 5º, XI (anonimização):** utilização de meios técnicos razoáveis e disponíveis no momento do tratamento.
- **Art. 13, § 4º (pseudonimização):** o tratamento pelo qual o dado perde a possibilidade de associação, direta ou indireta, a um indivíduo. O restante do dispositivo deve ser conferido no texto integral.
- **Art. 16 (eliminação):** os dados são eliminados após o fim do tratamento, com exceções previstas em incisos. Uma delas é o uso exclusivo do controlador com dados anonimizados.
- **Art. 46 (segurança):** medidas técnicas e administrativas contra acessos não autorizados e contra destruição, perda, alteração ou comunicação indevida.
- **Art. 48 (incidentes):** comunicação à autoridade nacional e ao titular quando o incidente puder causar risco ou dano relevante.

### 3.2 Consequência para o projeto

O `relato_hash` é calculado com HMAC-SHA256 e a chave fica fora do banco. Isso se encaixa na definição
de pseudonimização do art. 13, § 4º, porque a associação depende de uma informação mantida separadamente.
Enquanto a chave existir, o hash continua associado ao relato. Por isso, o projeto não o trata como dado
anonimizado (art. 5º, III). A retenção de 30 dias da resposta bruta e a regra de não gravar o relato
completo continuam necessárias.

Esta análise é técnica e não substitui parecer jurídico. A adequação deve ser revisada por profissional
habilitado antes de qualquer uso real com dados de clientes.

---

## 4. Arquitetura

| Tema | Fonte | Link ou referência | Usado em | Situação em 04/11/2026 |
| ---- | ----- | ------------------ | -------- | ---------------------- |
| Arquitetura limpa: regras de negócio sem dependência de infraestrutura | Robert C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Prentice Hall, 2017 | Livro impresso | [01](01-Contexto.md), seção 3.2; [06](06-Arquitetura-solucao.md) | Bibliografia, não consultada online |
| Organização de soluções e arquitetura .NET | Microsoft Learn | [learn.microsoft.com, arquitetura .NET](https://learn.microsoft.com/dotnet/architecture/microservices) | [06](06-Arquitetura-solucao.md), como referência geral | Localizado. O projeto não é de microsserviços; o guia serve apenas de referência geral |

---

## 5. Gemini e Docker (conferidas na versão 1.0, em 30/10/2026)

Estas fontes foram conferidas na versão anterior e não foram reconferidas nesta versão.

| Tema | Fonte | Link | Usado em |
| ---- | ----- | ---- | -------- |
| Saída estruturada do Gemini | Google, Gemini API | [ai.google.dev, structured output](https://ai.google.dev/gemini-api/docs/structured-output) | [06](06-Arquitetura-solucao.md) |
| Limites de uso do Gemini | Google, Gemini API | [ai.google.dev, rate limits](https://ai.google.dev/gemini-api/docs/rate-limits) | [02](02-Especificacao.md), [03](03-Metodologia.md) |
| Instalação do Docker Desktop no Windows | Docker | [docs.docker.com, Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/) | [03](03-Metodologia.md), [12](12-Manual-instalacao.md) |

A saída estruturada do Gemini reduz, mas não elimina, a necessidade de validação em código. O projeto mantém
a validação em `ValidadorSaidaLlm`.

---

## 6. Pendências de conferência antes da entrega

Estes itens precisam ser lidos na fonte oficial. Até lá, não entram no relatório como fato conferido.

- **PostgreSQL 17:** documentação oficial e notas da versão. Confirmar a versão usada na imagem Docker.
- **Npgsql e EF Core:** provedor PostgreSQL e documentação de migrações. Confirmar a versão dos pacotes fixada no `csproj`.
- **Telegram Bot API:** método `setWebhook` e cabeçalho `X-Telegram-Bot-Api-Secret-Token`.
- **Gemini, regra do nível gratuito:** se o provedor pode usar as requisições para melhorar produtos. Ver a premissa da seção 7 do [contexto](01-Contexto.md).
- **ANPD:** situação final do guia de anonimização e pseudonimização.
- **Programa GenAI do iFood:** data limite de inscrição (17/11/2026), segundo o Seja Trainee, na página oficial da vaga.
