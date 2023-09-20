using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Tavis.UriTemplates;
using NOA.Common.Service.Model;
using Microsoft.Extensions.Options;

namespace NOA.Common.Service
{
    public class AuthenticationService : IAuthenticationService
    {
        private static IPublicClientApplication _identityClientApp;
        private static AzureAdOptions _adOptions;

        public AuthenticationService(IOptions<AzureAdOptions> adOptions)
        {
            _adOptions = adOptions.Value;

            _identityClientApp = PublicClientApplicationBuilder
                .Create(_adOptions.ClientId)
                .WithTenantId (_adOptions.TenantId)
                .Build();
        }


        /// <summary>  
        /// Get Token for User.  
        /// </summary>  
        /// <returns>Token for user.</returns>  
        public  async Task<string> GetTokenForUserAsync(string[] scopes, string userPrincipalName)
        {
            string _tokenForUser = null;

            try
            {
                var cachePopulationResult = await _identityClientApp
                                                        .AcquireTokenInteractive(scopes)
                                                        .ExecuteAsync();

                var accounts = await _identityClientApp.GetAccountsAsync();
                var account = accounts.FirstOrDefault(x => x.Username == userPrincipalName);

                AuthenticationResult authResult = _identityClientApp
                    .AcquireTokenSilent(scopes, account)
                    .WithForceRefresh(true)
                    .ExecuteAsync().Result;

                _tokenForUser = authResult.AccessToken;
            }

            catch (Exception ex)
            {
                throw;
            }

            return _tokenForUser;
        }

        /// <summary>  
        /// Signs the user out of the service.  
        /// </summary>  
        public void SignOut(string userPrincipalName)
        {
            var user = _identityClientApp
                .GetAccountsAsync()
                .Result.First(x => x.Username == userPrincipalName);

            _identityClientApp.RemoveAsync(user);
        }

        /// <summary>
        /// Indicates of the AT presented was for an app-only token or not.
        /// </summary>
        /// <returns></returns>
        public bool IsAppOnlyToken(ClaimsPrincipal principal)
        {
            // Add in the optional 'idtyp' claim to check if the access token is coming from an application or user.
            //
            // See: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-optional-claims

            //var principal = GetCurrentClaimsPrincipal();
            if (principal != null)
            {
                return principal.Claims.Any(c => c.Type == "idtyp" && c.Value == "app");
            }

            return false;
        }

        /// <summary>
        /// returns the current claimsPrincipal (user/Client app) dehydrated from the Access token
        /// </summary>
        /// <returns></returns>
        public ClaimsPrincipal GetCurrentClaimsPrincipal(IHttpContextAccessor httpContextAccessor)
        {
            // Irrespective of whether a user signs in or not, the AspNet security middle-ware dehydrates the claims in the
            // HttpContext.User.Claims collection

            return httpContextAccessor?.HttpContext?.User;

        }
    }
}