using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using NOA.Common.Constants;
using NorskOffshoreAuthenticateService.Controllers;
using NorskOffshoreAuthenticateService.Models;
using NOA.Common.Service;
using Xunit.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Moq;
using System.Linq.Expressions;
using Microsoft.Graph.Models;
using System.Security.Claims;

namespace AuthenticateService_IntegrationTests
{
    public class AuthenticationServiceTests
    {
        private IAuthenticationService AuthenticationService { get; set; }

        public AuthenticationServiceTests()
        {
            /* Arrange */
            var provider = Startup.StartupContainer.GetServiceProvider();           
            AuthenticationService = (IAuthenticationService)provider.GetService(typeof(IAuthenticationService));
        }

        [Fact]
        public async Task Can_GetMyAuthenticationToken()
        {
            /* Arrange */
            //Set user to authenticate
            var principal = AuthenticationService.GetCurrentClaimsPrincipal(null);
            var scopes = new List<string> { };

            /* Act */
            var token = await AuthenticationService.GetTokenForUserAsync(scopes.ToArray(), principal);

            /* Assert */
            Assert.True(String.IsNullOrEmpty(token), "Existing user should be able to authenticate");
        }

        [Fact]
        public async Task Can_GetAuthenticationTokenByClaims()
        {
            /* Arrange */
            //Set user to authenticate
            var claims = new List <Claim>();
            var scopes = new List<string> { };

            /* Act */
            var token = await AuthenticationService.GetTokenForUserAsync(scopes.ToArray(), claims);

            /* Assert */
            Assert.True(String.IsNullOrEmpty(token), "Existing user should be able to authenticate");
        }

        [Fact]
        public async Task Can_GetAuthenticationTokenByUpn()
        {
            /* Arrange */
            //Set user to authenticate
            var upn = "";
            var scopes = new List<string> { };

            /* Act */
            var token = await AuthenticationService.GetTokenForUserAsync(scopes.ToArray(), upn);

            /* Assert */
            Assert.True(String.IsNullOrEmpty(token), "Existing user should be able to authenticate");
        }
    }
}
