# 01 — Contexto

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (30/10/2026), que descrevia a triagem como fluxo em n8n

---

## 1. Introdução

A oficina de motos recebe pedidos de serviço no balcão e por telefone. O cliente descreve o
problema do jeito que fala, e o atendente transcreve essa descrição para uma ficha de serviço.
O projeto **Triagem de Ordens de Serviço** automatiza essa etapa. Recebe o relato em texto livre
e devolve uma ficha estruturada, uma pergunta quando faltar informação ou um aviso de urgência
alta.

Na versão 1.0, a triagem rodava em um fluxo do n8n. Na versão 2.0, ela é uma **API REST
autônoma em C# / .NET**, construída com Minimal APIs, com PostgreSQL como base de dados. O n8n
saiu do caminho de execução e não é mais necessário para rodar o sistema.

A extração e a classificação do texto são feitas pelo **Google Gemini**, acessado pela API
HTTP do modelo. As regras de negócio e as validações ficam em **código C#**, no núcleo de
domínio da API. O banco reforça parte dessas regras com restrições de integridade.

Esse desenho separa três responsabilidades:

> O Gemini interpreta o texto. O código garante as regras. O banco reforça as regras.

O modelo pode errar a classificação, mas não decide sozinho se uma ficha está completa, se a
urgência deve subir nem se a ficha pode ser gravada.

---

## 2. Problema

A triagem atual é manual, e isso causa três problemas.

| Situação atual | Problema | Consequência |
| -------------- | -------- | ------------ |
| O atendente transcreve o relato do jeito que o cliente conta | A informação chega incompleta ou ambígua ao mecânico | Retrabalho, perguntas de volta ao cliente e demora no orçamento |
| A urgência é avaliada na intuição | Um caso de risco, como freio fraco ou vazamento de óleo, pode passar como caso comum | Risco à segurança de quem pilota, o erro mais grave do domínio |
| A mensagem ao cliente é escrita do zero a cada vez | Consome tempo e produz tons diferentes | Atendimento mais lento e pouco padronizado |

A causa comum é a entrada sem estrutura. Sem campos mínimos, como modelo e problema, cada pedido
exige interpretação manual, e os casos graves dependem da atenção de quem atende.

---

## 3. Objetivos

### 3.1 Objetivo geral

Entregar até **16/11/2026** uma API de triagem funcional, testada com 24 casos com gabarito, com
a precisão medida e documentada por versão do prompt.

### 3.2 Objetivos específicos

- **Estruturar a entrada.** Extrair do texto livre, em JSON validado, o modelo da moto, o ano, os
  sintomas, a categoria do serviço, a urgência e as peças prováveis.
- **Proteger os casos de risco.** Garantir, por regra em código e por restrição no banco, que um
  relato com palavra de risco nunca fique abaixo da urgência alta.
- **Separar IA e regra de negócio.** Restringir o Gemini à extração, à classificação e à redação
  da mensagem. Manter as validações em um núcleo de domínio sem dependências de infraestrutura.
- **Medir a qualidade.** Comparar a saída do Gemini com o gabarito e registrar cada chamada, com
  versão do prompt, modelo, temperatura e tempo.
- **Ser reproduzível e seguro.** Rodar com custo zero, PostgreSQL em Docker Compose, migrações
  versionadas, segredos fora do repositório, HMAC-SHA256 no hash do relato, retenção de 30 dias
  para a resposta bruta e canal do Telegram em modo fail closed.

### 3.3 Objetivo de portfólio

O projeto também serve como peça de portfólio para o Programa de Estágio GenAI do iFood. A
documentação deve mostrar, com evidências, o aprendizado em:

- engenharia de prompt, com versões do prompt e uma mudança por vez;
- saída estruturada de LLM validada por código e por restrições de banco;
- API em .NET com Minimal APIs, contrato OpenAPI e DTOs explícitos;
- medição objetiva de qualidade, com gabarito escrito antes da execução;
- testes em níveis (unitário, integração e avaliação) e reprodução em Docker.

As inscrições do programa vão até **17/11/2026**, segundo o Seja Trainee. A data ainda precisa ser
confirmada na página oficial da vaga.

### 3.4 Entregáveis

- repositório `os-triage-api` com a solução .NET, as migrações e o `docker-compose.yml`;
- contrato OpenAPI em `docs/openapi.yaml`;
- suítes de teste automatizado e relatório de testes com as métricas de precisão por versão do
  prompt;
- README com instalação e execução, conforme o [manual](12-Manual-instalacao.md);
- vídeo de demonstração de até 3 minutos.

---

## 4. Escopo da versão 2.0

### 4.1 Dentro do escopo

- Endpoint de registro de relato, `POST /api/triagem` (RF01).
- Extração, classificação e redação com o Gemini, com validação em código (RF02 a RF05, RF07).
- Gravação com idempotência e rastreio por execução (RF06, RF10).
- Consulta de ordens com filtros e fila por urgência (RF13).
- Canal do Telegram, em modo síncrono e fail closed (RF14).
- Contrato OpenAPI e teste de contrato (RF15).

### 4.2 Fora do escopo

- Interface visual própria. A versão 1 da API é só API.
- Lista de peças por agente (RF12, versão 2).
- Cobrança, preços, orçamento automático e integração com sistema de gestão da oficina.
- Envio automático ao cliente por WhatsApp. A mensagem continua sendo enviada pelo atendimento.
- Transições de status das ordens.
- Autenticação de usuários. A API roda em rede local.

---

## 5. Mudança de arquitetura: de n8n para API

| Aspecto | Versão 1.0 (n8n) | Versão 2.0 (API .NET) | Motivo |
| ------- | ---------------- | --------------------- | ------ |
| Orquestração | Fluxos visuais do n8n | Minimal API em C# | Regras testáveis em código, sem dependência de nós visuais |
| Entrada | Formulário do n8n | `POST /api/triagem` e webhook do Telegram | Contrato HTTP explícito, com DTOs e códigos de status |
| Chamada ao Gemini | Nó do n8n | Cliente HTTP tipado atrás de `ILlmClient` | Troca de modelo por configuração, sem alterar o código |
| Persistência | Planilha ou CSV | PostgreSQL com restrições | Integridade, concorrência e consulta por índice |
| Testes | Fluxo de avaliação do n8n | Suítes xUnit (unitária, integração, contrato e avaliação) | Testes automatizados e reproduzíveis |
| Execução | n8n em Docker | API e PostgreSQL em Docker Compose | Ambiente local reproduzível |

A decisão está registrada em detalhe na [arquitetura](06-Arquitetura-solucao.md), seção 5.2. A
versão 1.0 previa essa evolução na seção 9 do seu documento de arquitetura, como caminho futuro.

---

## 6. Público-alvo

| Público | Relação com o sistema |
| ------- | --------------------- |
| Atendimento da oficina | Registra o pedido pelo cliente REST ou pelo bot do Telegram e recebe a ficha, a pergunta ou o aviso de urgência |
| Mecânicos | Consultam as ordens gravadas por API, com filtros de urgência e de categoria |
| Clientes da oficina | Recebem a mensagem enviada pelo atendimento. Não acessam o sistema |
| Desenvolvedor | Constrói, testa e mantém a API, o prompt e o conjunto de teste |
| Avaliadores do portfólio | Leem a documentação, executam a API e assistem ao vídeo para verificar a solução |

As personas que detalham esses públicos estão em [02-Especificacao.md](02-Especificacao.md).

---

## 7. Premissas e restrições

- O projeto custa R$ 0: .NET, PostgreSQL em Docker e nível gratuito da Gemini API.
- O nível gratuito limita requisições por minuto, por tokens e por dia. Os testes de avaliação
  rodam em lotes com pausa.
- O nível gratuito pode permitir que o provedor use as requisições para melhorar os produtos. Por
  isso, somente dados fictícios entram no sistema.
- O envio da mensagem ao cliente é manual, feito pelo atendimento.
- A API roda na máquina do desenvolvedor, escutando somente em `127.0.0.1`.
- O teste do canal Telegram exige um endereço HTTPS público, obtido por túnel durante o teste.
- Prazo: a entrega é **16/11/2026**, e o prazo de ajuste de testes é **12/11/2026**. A migração
  para .NET não estava no plano original e reduz a folga disponível (ver
  [metodologia](03-Metodologia.md), seção 2).

---

## 8. Conjunto de documentos

| Documento | Conteúdo | Situação na versão 2.0 |
| --------- | -------- | ---------------------- |
| [01-Contexto](01-Contexto.md) | Problema, objetivos e escopo | Atualizado |
| [02-Especificacao](02-Especificacao.md) | Personas, casos de uso, requisitos e regras | Atualizado |
| [03-Metodologia](03-Metodologia.md) | Fases, cronograma e ferramentas | Atualizado |
| [04-Projeto-interface](04-Projeto-interface.md) | Contratos da API, DTOs e códigos de status | Atualizado |
| [05-Template-padrao](05-Template-padrao.md) | Modelo para documentar sub-fluxos | Pendente: referenciava sub-fluxos do n8n |
| [06-Arquitetura-solucao](06-Arquitetura-solucao.md) | Modelo de dados, segurança, LGPD e arquitetura | Atualizado |
| [07-Plano-testes-software](07-Plano-testes-software.md) | Estratégia de teste e gabarito congelado | Atualizado |
| [08-Registro-testes-software](08-Registro-testes-software.md) | Resultados das rodadas | Pendente: formato em planilha do n8n |
| [09-Plano-testes-usabilidade](09-Plano-testes-usabilidade.md) | Testes de usabilidade | Pendente: referenciava o formulário do n8n |
| [10-Registro-testes-usabilidade](10-Registro-testes-usabilidade.md) | Resultados de usabilidade | Pendente |
| [11-Referencias](11-Referencias.md) | Referências externas | Pendente de revisão |
| [12-Manual-instalacao](12-Manual-instalacao.md) | Instalação e execução | Atualizado |