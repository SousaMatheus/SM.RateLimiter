using Microsoft.AspNetCore.RateLimiting;
using MS.RateLimiting.Services;
using System.Threading.RateLimiting;
using MS.RateLimiting.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromSeconds(5);
        limiter.QueueLimit = 2;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddPolicy("por-ip", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(5)
            }));

    options.AddPolicy("por-usuario", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.User.Identity?.Name ?? "usuario-anonimo",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(5),
                AutoReplenishment = true,
                QueueLimit = 0
            }));

    options.AddPolicy("por-plano", context =>
    {
        var chave = context.Request.Headers["X-API-KEY"].ToString();
        var cliente = PlanServices.ResolverCliente(chave);

        return cliente.Plano switch
        {
            PlanoEnum.Enterprise => RateLimitPartition.GetTokenBucketLimiter(cliente.Id, _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5000,
                    TokensPerPeriod = 500,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true
                }),
            PlanoEnum.Pro => RateLimitPartition.GetTokenBucketLimiter(cliente.Id, _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1000,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetFixedWindowLimiter(cliente.Id, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                })
        };
    });

});

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapGet("/demo/fixed", () => "fixed window")
    .RequireRateLimiting("fixed");

app.MapGet("/demo/ip", () => "limite por IP")
    .RequireRateLimiting("por-ip");

app.MapGet("/demo/usuario", () => "token bucket por usuário")
    .RequireRateLimiting("por-usuario");

app.MapGet("/demo/plano", (HttpContext context) =>
    $"Plano: {PlanServices.ResolverCliente(context.Request.Headers["X-API-KEY"].ToString()).Plano}")
    .RequireRateLimiting("por-plano");

app.Run();
