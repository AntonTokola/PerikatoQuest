using System.Security.Claims;

namespace Perikato.Services
{

    //Henkilötietojen siirtoa varten OIDC-prosessista "CustomData" - API-endpointiin
    public interface IUserClaimsService
    {
        ClaimsPrincipal CurrentUser { get; set; }
    }

    public class UserClaimsService : IUserClaimsService
    {
        public ClaimsPrincipal CurrentUser { get; set; }
    }

}
