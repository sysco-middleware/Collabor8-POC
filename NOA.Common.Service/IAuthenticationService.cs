using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NOA.Common.Service
{
    public interface IAuthenticationService
    {
        Task<string> GetTokenForUserAsync(string[] scopes, string userPrincipalName);
        void SignOut(string userPrincipalName);
        bool IsAppOnlyToken(ClaimsPrincipal principal);
        ClaimsPrincipal GetCurrentClaimsPrincipal(IHttpContextAccessor httpContextAccessor);
    }
}
