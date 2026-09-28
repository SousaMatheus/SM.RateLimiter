# MS.RateLimiting

Projeto de estudo sobre controle de requisições no ASP.NET Core 10. Os exemplos mostram como escolher algoritmos e particionar cotas por IP, identidade e plano de cliente.

## Cenários disponíveis

| Endpoint | Estratégia | Limite demonstrado |
| --- | --- | --- |
| `GET /demo/fixed` | Fixed window | 3 requisições a cada 5 s, com fila de 2 |
| `GET /demo/ip` | Fixed window particionado | 5 requisições a cada 5 s por IP |
| `GET /demo/usuario` | Token bucket particionado | Balde de 5 tokens, reposição de 10 a cada 5 s |
| `GET /demo/plano` | Token bucket ou fixed window por plano | Enterprise: 5000 + 500/10 s; Pro: 1000 + 100/10 s; Gratuito: 60/min |

Todas as rotas também compartilham um limite global de 300 requisições por minuto por IP. Quando uma cota é excedida, a API responde com `429 Too Many Requests`.

## Executar

Requisitos: .NET 10 SDK.

```bash
dotnet restore
dotnet run
```

Use o endereço informado no terminal. O arquivo `MS.RateLimiter.http` contém uma requisição para cada cenário. No exemplo por plano, use `123` para Enterprise, `456` para Pro ou `789` para Gratuito.

## Observações

- As chaves são fictícias para estudo. Chaves desconhecidas compartilham a partição gratuita; o header sozinho não autentica o cliente.
- Sem autenticação configurada, as chamadas ao exemplo por usuário compartilham a partição anônima. Com autenticação, `UseAuthentication` deve vir antes de `UseRateLimiter`.
- O exemplo por IP usa o endereço remoto da conexão. Atrás de proxy, configure encaminhamento confiável antes de usar o endereço encaminhado.
- O limiter guarda estado em memória no processo; instâncias diferentes mantêm contadores independentes.
- Rate limiting na aplicação ajuda a controlar consumo, mas não substitui proteção de rede contra ataques distribuídos.

Este repositório é didático. Os valores de limite e as chaves não são recomendações para produção.
