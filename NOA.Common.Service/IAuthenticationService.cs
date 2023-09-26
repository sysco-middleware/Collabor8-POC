using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NOA.Common.Service
{
    public interface IAuthenticationService
    {
        Task<string> GetTokenForUserAsync(string[] scopes, ClaimsPrincipal principal);
        Task<string> GetTokenForUserAsync(string[] scopes, string userPrincipalName);
        Task<string> GetTokenForUserAsync(string[] scopes, List<Claim> claims);
        void SignOut(string userPrincipalName);
        bool IsAppOnlyToken(ClaimsPrincipal principal);
        ClaimsPrincipal GetCurrentClaimsPrincipal(IHttpContextAccessor httpContextAccessor);
        ClaimsPrincipal GetOtherClaimsPrinciple(List<Claim> claims);
    }
}
