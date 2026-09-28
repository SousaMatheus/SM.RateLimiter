using Microsoft.AspNetCore.RateLimiting;
using MS.RateLimiting.Services;
using System.Threading.RateLimiting;
using MS.RateLimiting.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//LIMITAÇÃO POR ESTADO GLOBAL, NÃO POR USUÁRIO. 
//FIXED WINDOW LIMITER: PERMITE 3 REQUISIÇÕES A CADA 5 SEGUNDOS, COM FILA DE 2 REQUISIÇÕES.
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("fixed", config =>
    {
        config.PermitLimit = 3;
        config.Window = TimeSpan.FromSeconds(5);
        config.QueueLimit = 2;
    });

    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});


//GLOBAL LIMITER PARTICIONADO POR IP, PODE SOFRER SE O CLIENTE USA PROXY REVERSO POIS SERA ATRIBUIDO O MESMO IP PARA VARIOS USUARIOS
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(5)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

//GLOBAL LIMITER PARTICIONADO POR USUARIO USANDO TOKEN BUCKET
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User.Identity?.Name ?? "desconhecido",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(5),
                AutoReplenishment = true,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});


//LIMITER PARTICIONADO POR PLANO
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("por-plano", contexto =>
    {
        var chave = contexto.Request.Headers["X-API-KEY"].ToString();

        if(string.IsNullOrWhiteSpace(chave))
        {
            return RateLimitPartition.GetFixedWindowLimiter("sem-chave", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(5)
            });
        }

        var plano = PlanServices.ResolverPlano(chave);

        return plano switch
        {
            PlanoEnum.Enterprise => RateLimitPartition.GetTokenBucketLimiter(chave, _ =>

                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5000,
                    TokensPerPeriod = 500,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true
                }),

            PlanoEnum.Pro => RateLimitPartition.GetTokenBucketLimiter(chave, _ =>

                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1000,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true
                }),

            _ => RateLimitPartition.GetFixedWindowLimiter(chave, _ =>

                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromSeconds(60)
                })
        };
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

//VÁRIOS LIMITERS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1)
        }));

    options.AddFixedWindowLimiter("login", limitador =>
    {
        limitador.PermitLimit = 5;
        limitador.Window = TimeSpan.FromMinutes(1);
    });

    options.AddConcurrencyLimiter("relatorio", limitador =>
    {
        limitador.PermitLimit = 5;
        limitador.QueueLimit = 10;
        limitador.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

//LIMITES ENCADEADOS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(

    PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: "chave-do-cliente",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        })),

    PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
    RateLimitPartition.GetFixedWindowLimiter(
        "chave-do-cliente",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
        })));
});

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

//RateLimiter after Authentication/Authorization
app.UseRateLimiter();

app.MapGet("/products", () =>
    "product"
)
.RequireRateLimiting("fixed")
.WithName("products");

app.Run();