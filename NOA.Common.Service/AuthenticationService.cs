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
using Microsoft.Identity.Web;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Extensions.Logging;

namespace NOA.Common.Service
{
    public class AuthenticationService : IAuthenticationService
    {
        private ILogger _logger;
        private readonly ITokenAcquisition _tokenAcquisition;
        private static IPublicClientApplication _identityClientApp;
        private static AzureAdOptions _adOptions;

        public AuthenticationService(IOptions<AzureAdOptions> adOptions, ITokenAcquisition tokenAcquisition, ILogger logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _adOptions = adOptions.Value;

            _identityClientApp = PublicClientApplicationBuilder
                .Create(_adOptions.ClientId)
                .WithTenantId (_adOptions.TenantId)
                .Build();
        }

        public async Task<string> GetTokenForUserAsync(string[] scopes, List<Claim> claims)
        {
            try
            {
                return await GetToken(scopes, claims);
            }
            catch(Exception ex) {
                _logger.LogError($"Caught general exception with real type '{ex.GetType()}' holding message: {ex.Message + ex.InnerException}");
                throw;
            }
        }

        public async Task<string> GetTokenForUserAsync(string[] scopes, ClaimsPrincipal principal)
        {
            try
            {
                return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, _adOptions.TenantId, default, principal);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Caught general exception with real type '{ex.GetType()}' holding message: {ex.Message + ex.InnerException}");
                throw;
            }
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
            } catch (Exception ex1)
            {
                try
                {
                    _tokenForUser = await GetToken(scopes, new List<Claim>
                        {
                            new Claim(ClaimTypes.Upn, userPrincipalName)
                        });
                } catch(Exception ex2)
                {
                    _logger.LogError($"Caught general exception with real type '{ex2.GetType()}' holding message: {ex2.Message + ex2.InnerException}");
                    throw;
                }
                _logger.LogError($"Caught general exception with real type '{ex1.GetType()}' holding message: {ex1.Message + ex1.InnerException}");
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

        public ClaimsPrincipal GetOtherClaimsPrinciple(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "custom-auth-type");
            return new ClaimsPrincipal(identity);
        }

        private async Task<string> GetToken(string[] scopes, List<Claim> claims)
        {
            try
            {
                var otherUser = GetOtherClaimsPrinciple(claims);
                return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, _adOptions.TenantId, default, otherUser);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Caught general exception with real type '{ex.GetType()}' holding message: {ex.Message + ex.InnerException}");
                throw;
            }
        } 
    }
}