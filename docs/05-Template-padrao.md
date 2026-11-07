# 05 — Template padrão para componentes da API

- **Versão:** 2.2
- **Data da última mudança:** 07/11/2026
- **Substitui:** versão 2.1 (05/11/2026)

---

## 1. Como usar

1. Copie o bloco da seção 2 para `docs/componentes/NN-nome-do-componente.md`.
2. Classifique o componente como regra de domínio, serviço de aplicação, endpoint REST, persistência ou integração externa.
3. Informe o tipo C#, o caminho real do fonte e os testes xUnit correspondentes.
4. Referencie requisitos, casos `CT-NN` e testes especiais `TE-NN` sem duplicar o texto do gabarito.
5. Para endpoints, registre método, rota, códigos HTTP e formato ProblemDetails.
6. Para integrações, registre timeout, retentativas, configuração e tratamento de falhas.
7. Segredos aparecem somente pelo nome da chave de configuração, nunca pelo valor.
8. Atualize o histórico quando o comportamento ou o contrato mudar.

---

## 2. Modelo em branco

```markdown
# NN — Nome do componente

- **Tipo:** regra de domínio | serviço de aplicação | endpoint REST | persistência | integração externa
- **Identificador:** `Namespace.NomeDoTipo`
- **Arquivo fonte:** `src/OsTriageApi/caminho/Arquivo.cs`
- **Testes:** `tests/OsTriageApi.Tests.Unit/caminho/ArquivoTests.cs`
- **Versão:** número da versão
- **Data da última mudança:** DD/MM/AAAA

## 1. Propósito
Descreva o que o componente faz e o limite de sua responsabilidade.

## 2. Contrato de entrada
| Campo | Tipo | Obrigatório | Origem | Regra |
| ----- | ---- | ----------- | ------ | ----- |

## 3. Contrato de saída
| Campo | Tipo | Descrição |
| ----- | ---- | --------- |

## 4. Regras aplicadas
Liste os requisitos e as regras da seção 6 da especificação.

## 5. Erros e tratamentos
| Situação | Tratamento | Status HTTP ou código interno |
| -------- | ---------- | ----------------------------- |

## 6. Dependências e configuração
Liste interfaces, serviços registrados na injeção de dependência e chaves de configuração.

## 7. Como testar
Liste testes xUnit, casos do gabarito e critério de aprovação.

## 8. Histórico de versões
| Versão | Data | Mudança |
| ------ | ---- | ------- |
```

---

## 3. Exemplo preenchido: `DecisorTriagem`

- **Tipo:** regra de domínio
- **Identificador:** `OsTriageApi.Relatos.Validacao.DecisorTriagem`
- **Arquivo fonte:** `src/OsTriageApi/Relatos/Validacao/DecisorTriagem.cs`
- **Testes:** `tests/OsTriageApi.Tests.Unit/Relatos/DecisorTriagemTests.cs`
- **Versão:** 1.0
- **Data da última mudança:** 07/11/2026

### 3.1 Propósito

Receber uma `FichaExtraida` já validada, confirmar o ano no relato e aplicar `RegrasFicha`. O
componente não acessa HTTP, Gemini ou PostgreSQL.

### 3.2 Contrato

| Entrada | Tipo | Regra |
| ------- | ---- | ----- |
| `ficha` | `FichaExtraida` | Saída aprovada por `ValidadorSaidaLlm` |
| `relato` | string | Texto já aprovado por `RelatoValidador` |

| Saída | Tipo | Descrição |
| ----- | ---- | --------- |
| `Ficha` | `FichaExtraida` | Ficha com o ano confirmado ou nulo |
| `Regras` | `ResultadoRegrasFicha` | Informa se a ficha é gravável, campos faltantes e urgência final |
| `CamposNaoConfirmados` | `IReadOnlyList<string>` | Contém `ano` quando o valor extraído não aparece no relato |

### 3.3 Regras e testes

- Modelo e problema são obrigatórios; ano é opcional.
- Palavra de risco só eleva a urgência.
- Categoria `outro ou não identificado` não exige detalhe.
- A confirmação textual do modelo permanece fora desta etapa.
- Testes: `Avaliar_AnoForaDoRelato_RemoveAnoERegistraCampo` e
  `Avaliar_RiscoElevaUrgenciaSemAlterarUrgenciaExtraida`.

---

## 4. Histórico deste documento

| Versão | Data | Mudança |
| ------ | ---- | ------- |
| 2.2 | 07/11/2026 | Atualiza o modelo para componentes C#, contratos REST e o `DecisorTriagem` real. |
| 2.1 | 05/11/2026 | Atualiza os caminhos de testes para `OsTriageApi.Tests.Unit`. |
| 2.0 | 04/11/2026 | Adapta o template aos componentes da API .NET. |
