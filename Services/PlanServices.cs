using MS.RateLimiting.Enums;

namespace MS.RateLimiting.Services;

public static class PlanServices
{
    public static PlanoEnum ResolverPlano(string chave)
    {
        //verificar na BD qual o plano do cliente, se estiver cadastrado.
        switch(chave)
        {
            case "123":
                return PlanoEnum.Enterprise;
            case "456":
                return PlanoEnum.Pro;
            default:
                return PlanoEnum.Gratuito;
        }
    }
}