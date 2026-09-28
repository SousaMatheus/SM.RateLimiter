using MS.RateLimiting.Enums;

namespace MS.RateLimiting.Services;

public sealed record ClientePlano(string Id, PlanoEnum Plano);

public static class PlanServices
{
    private const string ClienteEnterprise = "cliente-enterprise";
    private const string ClientePro = "cliente-pro";
    private const string ClienteGratuito = "cliente-gratuito";

    // Credenciais fictícias para demonstração. Não usar como autenticação em produção.
    public static ClientePlano ResolverCliente(string? chave) => chave?.Trim() switch
    {
        "123" => new ClientePlano(ClienteEnterprise, PlanoEnum.Enterprise),
        "456" => new ClientePlano(ClientePro, PlanoEnum.Pro),
        "789" => new ClientePlano(ClienteGratuito, PlanoEnum.Gratuito),
        _ => new ClientePlano("cliente-anonimo", PlanoEnum.Gratuito)
    };
}
