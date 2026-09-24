# MS.RateLimiting

> Estudo de rate limiting com ASP.NET Core e C#.

Este projeto explora formas de controlar a quantidade de requisições recebidas por uma API. A proposta é entender os algoritmos de limitação, como aplicar políticas no pipeline do ASP.NET Core e como separar limites por cliente ou identidade.

## 🎯 Objetivos do estudo

- Entender o middleware de rate limiting do ASP.NET Core.
- Comparar estratégias como janela fixa e token bucket.
- Aplicar políticas globais e políticas específicas por endpoint.
- Explorar partições por IP, usuário, API key e plano de assinatura.
- Observar respostas `429 Too Many Requests` quando o limite é excedido.

## 🧩 Conceitos explorados

### Fixed window

Permite um número definido de requisições por janela de tempo. Ao iniciar uma nova janela, a quantidade permitida é renovada.

### Token bucket

Cada requisição aceita consome um token. Os tokens são repostos ao longo do tempo até a capacidade máxima do balde, permitindo rajadas curtas de chamadas.

### Particionamento

Uma partição mantém um limite separado para cada chave escolhida, como o identificador de um usuário ou cliente. A chave deve vir de uma identidade validada pela aplicação.

## 🛠 Tecnologias

- C#
- ASP.NET Core
- Middleware de rate limiting

## ▶️ Executando localmente

Requisitos: .NET 10.

No terminal, entre na pasta do projeto e execute:

```bash
dotnet restore
dotnet run
```

Use o endereço informado no terminal para chamar a API. O projeto inclui um arquivo `.http`, abra-o no Visual Studio 2026 e use clique em **Send Request** para enviar as requisições de exemplo.

## 🧪 Testando os limites

Envie chamadas repetidas ao endpoint configurado com a política de rate limiting. As chamadas permitidas recebem a resposta normal do endpoint; quando o limite é excedido, a API pode responder com `429 Too Many Requests`, conforme a configuração.

Para observar diferenças entre usuários ou clientes, teste com identidades ou chaves válidas distintas. Chamadas sem identidade, se aceitas, podem compartilhar uma partição de fallback.

## ⚠️ Observações

- Um limitador em memória mantém o estado apenas no processo atual da aplicação. Múltiplas instâncias possuem estados separados.
- Usar um header como `X-API-KEY` para escolher uma partição não valida a chave por si só; a aplicação precisa autenticar ou validar o cliente.
- O rate limiter do ASP.NET Core é útil para controlar o tráfego da aplicação, mas não substitui proteção de rede contra ataques distribuídos.

Este repositório tem finalidade de estudo. Os exemplos e políticas devem ser avaliados de acordo com a implementação e o ambiente de execução do projeto.
