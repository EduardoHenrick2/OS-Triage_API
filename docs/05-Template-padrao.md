# 05 — Template padrão para componentes da API

- **Versão:** 2.0
- **Data da última mudança:** 04/11/2026
- **Substitui:** versão 1.0 (30/10/2026), que documentava sub-fluxos de automação

---

## 1. Como usar

1. Copie o bloco da seção 2 para um arquivo novo em `docs/componentes/`, nomeado `NN-nome-do-componente.md`.
2. Escolha o tipo do componente no campo **Tipo**: serviço de domínio, validador, endpoint HTTP ou integração externa. O mesmo modelo serve aos quatro tipos.
3. Preencha todas as seções. Se uma seção não se aplicar, escreva "Não se aplica" e o motivo, em vez de apagá-la. Por exemplo, um validador puro não tem dependências de rede.
4. Referencie requisitos pelo ID (`RF01`, `RNF03`), casos de teste pelo ID do gabarito (`CT-01`) e testes especiais pelo ID (`TE01`), sem copiar o texto do requisito.
5. O identificador é o tipo C# com namespace e o caminho do arquivo fonte. Nunca use nome de fluxo visual.
6. Em endpoints, registre o status HTTP e o `type` do ProblemDetails (RFC 9457). Em saídas do LLM, registre o código de falha (`codigo_erro`), que é o mesmo usado em `MotivoFalha`.
7. Atualize o histórico de versões a cada mudança de comportamento, não a cada mudança de texto.
8. Não descreva ferramentas de automação legadas no corpo do componente. A migração fica registrada em [01-Contexto](01-Contexto.md), seção 5.

---

## 2. Modelo em branco

```markdown
# NN — Nome do componente

- **Tipo:** serviço de domínio | validador | endpoint HTTP | integração externa
- **Identificador:** `Namespace.NomeDoTipo`
- **Arquivo fonte:** `src/OsTriageApi/caminho/Arquivo.cs`
- **Testes:** `tests/OsTriageApi.Tests/caminho/ArquivoTests.cs`
- **Versão:** número da versão do documento
- **Data da última mudança:** DD/MM/AAAA

## 1. Propósito
Uma ou duas frases sobre o que o componente faz e por que ele existe.

## 2. Contrato de entrada
| Campo | Tipo | Obrigatório | Exemplo fictício | Origem |
| ----- | ---- | ----------- | ---------------- | ------ |

## 3. Contrato de saída
| Campo | Tipo | Descrição |
| ----- | ---- | --------- |

## 4. Regras aplicadas
IDs de requisito e de regra de negócio que o componente implementa, por exemplo RF04, RF05 e a seção 6.4 da [especificação](02-Especificacao.md).

## 5. Erros e tratamentos
| Situação | Tratamento | Resultado (status HTTP, `type` ou código de falha) |
| -------- | ---------- | -------------------------------------------------- |

## 6. Dependências
Interfaces (por exemplo `ILlmClient`), chaves de configuração (por exemplo `Gemini:Modelo`), tabelas e colunas, e serviços externos.
Segredos aparecem somente pelo nome da chave, nunca pelo valor.

## 7. Como testar
- Testes de unidade, com os nomes dos métodos xUnit.
- Caso de teste de referência do gabarito (`CT-NN`) ou teste especial (`TENN`), do [plano de testes](07-Plano-testes-software.md).
- Critério de aprovação.

## 8. Histórico de versões
| Versão | Data | Mudança |
| ------ | ---- | ------- |
```

---

## 3. Exemplo preenchido: `DecisorTriagem`

```markdown
# 01 — Decisor de triagem

- **Tipo:** serviço de domínio
- **Identificador:** `OsTriageApi.Relatos.Validacao.DecisorTriagem`
- **Arquivo fonte:** `src/OsTriageApi/Relatos/Validacao/DecisorTriagem.cs`
- **Testes:** `tests/OsTriageApi.Tests/Relatos/Validacao/DecisorTriagemTests.cs`
- **Versão:** 1.0
- **Data da última mudança:** 04/11/2026

## 1. Propósito
Decide o desfecho de um relato já extraído pelo LLM: gravar a ficha ou perguntar ao atendente.
Aplica a detecção de risco, a confirmação do ano e a verificação de campos obrigatórios, e calcula a
urgência final. Não faz chamada de rede, banco ou configuração, por isso é testado sem infraestrutura.

## 2. Contrato de entrada
| Campo | Tipo | Obrigatório | Exemplo fictício | Origem |
| ----- | ---- | ----------- | ---------------- | ------ |
| `relato` | string | Sim | "Honda CB 300 2017. O freio está mole." | `CriarRelatoRequest.Relato`, já validado por `ValidadorContratoRelato` |
| `saida` | `SaidaExtracaoLlm` | Sim | Categoria `Freios`, urgência `Media` | Resultado de `ValidadorSaidaLlm.Validar`, com `Valido = true` |

## 3. Contrato de saída
| Campo | Tipo | Descrição |
| ----- | ---- | --------- |
| `Resultado` | `ResultadoTriagem` | `Gravada` ou `Pergunta` |
| `CamposFaltando` | lista de `CampoObrigatorio` | `modeloMoto` e `problema`, quando ausentes |
| `UrgenciaFinal` | `Urgencia?` | Nulo quando o resultado é `Pergunta` |
| `TemRisco` | bool | Verdadeiro se o relato contém palavra de risco vigente |
| `ModeloMoto` | string? | Modelo informado pelo LLM |
| `Ano` | int? | Ano confirmado no texto do relato; nulo se não aparece |

## 4. Regras aplicadas
- RF04 (urgência, com piso de risco) e RF05 (campos obrigatórios).
- RF11 (texto do relato tratado como dado): o componente não interpreta instruções contidas no relato.
- RNF08 (validação determinística em código).
- Regra de palavras de risco vigentes (seção 6.5 da [especificação](02-Especificacao.md)). A proposta de ampliação não entra neste componente.

## 5. Erros e tratamentos
| Situação | Tratamento | Resultado |
| -------- | ---------- | --------- |
| Campo obrigatório ausente | Resultado `Pergunta`, sem urgência final | Endpoint responde 422 com `camposFaltando`; nada é gravado |
| Palavra de risco no relato | Urgência final `Alta`, independentemente da urgência do LLM | Ficha gravada com aviso de urgência alta |
| Ano do LLM não aparece no relato | Ano vira nulo | Ficha gravada sem ano |
| Saída do LLM inválida | Não chega a este componente; `ValidadorSaidaLlm` devolve o código de falha | Nova tentativa; 502 com `saida_llm_invalida` após 3 tentativas |

## 6. Dependências
- `DetectorRisco`, `ConfirmadorFicha` e `UrgenciaFinal`, no mesmo namespace.
- Nenhuma dependência de infraestrutura.

## 7. Como testar
- Testes de unidade (xUnit):
  - `Decidir_FichaCompletaComRisco_GravaComUrgenciaAlta`
  - `Decidir_SemModelo_PerguntaSemUrgenciaFinal`
  - `Decidir_SemRisco_MantemUrgenciaDoLlm`
- Caso de referência: CT-01 (pedido claro, deve gravar) e CT-13 (sem modelo, deve perguntar), do [plano de testes](07-Plano-testes-software.md).
- Critério de aprovação: os três testes de unidade passam e a decisão de CT-01 e CT-13 coincide com o gabarito.

## 8. Histórico de versões
| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 1.0 | 04/11/2026 | Primeira versão, com piso de risco e verificação de campos obrigatórios |
```
