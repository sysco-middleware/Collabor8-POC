using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using NorskOffshoreAuthenticateService.Controllers;
using NorskOffshoreAuthenticateService.Models;
using Xunit.DependencyInjection;

namespace AuthenticateService_IntegrationTests
{
    public class AuthenticateServiceTests
    {
        private UsersController _usersController { get; set; }
        public AuthenticateServiceTests()
        {
            /* Arrange */
            //Set up AuthenticateService for testing

            var provider = Startup.StartupContainer.GetServiceProvider();

            /*
             * ITokenAcquisition tokenAcquisition, 
             * IConfiguration configuration, 
             * IHttpContextAccessor httpContextAccessor
             */

            var ta = (ITokenAcquisition) provider.GetService(typeof(ITokenAcquisition));
            var co = (IConfiguration) provider.GetService(typeof(IConfiguration));
            var ca = (IHttpContextAccessor) provider.GetService(typeof(IHttpContextAccessor));
            var gc = (GraphServiceClient) provider.GetService(typeof(GraphServiceClient));
            var cca = (MicrosoftIdentityConsentAndConditionalAccessHandler) provider.GetService(typeof(MicrosoftIdentityConsentAndConditionalAccessHandler));

            _usersController = new UsersController(ta, co, ca, gc, cca);
        }

        [Fact]
        public async Task WhenAuthenticating_ExistingUser_ThenAuthenticationWorks()
        {
            /* Arrange */
            //Set user to authenticate
            string userMail = "existing_tom@corporation.org";

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await _usersController.AuthenticateUser(userMail);

            var authenticationResult = controllerResult.Value;

            /* Assert */
            Assert.True(authenticationResult, "Existing user should be able to authenticate");
        }

        [Fact]
        public async Task WhenAuthenticating_NonExistingUser_ThenAuthenticationFails()
        {
            /* Arrange */
            //Set user to authenticate
            string userMail = "missing_jonas@corporation.org";

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await _usersController.AuthenticateUser(userMail);

            var authenticationResult = controllerResult.Value;

            /* Assert */
            Assert.False(authenticationResult, "Missing user should not be able to authenticate");
        }

        [Fact]
        public async Task WhenQueryingUserStatus_OnNonExistingUser_ThenStatusSaysMissing()
        {
            /* Arrange */
            //Set user to authenticate
            string userMail = "missing_jonas@corporation.org";

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await _usersController.GetUserStatus(userMail);

            var statusResult = controllerResult.Value;

            /* Assert */
            Assert.False(statusResult == UserStatus.Missing, "Missing user should return status as missing");
        }

        [Fact]
        public async Task WhenQueryingUserStatus_OnExistingUser_ThenStatusSaysExists()
        {
            /* Arrange */
            //Set user to authenticate
            string userMail = "existing_tom@corporation.org";

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await _usersController.GetUserStatus(userMail);

            var statusResult = controllerResult.Value;

            /* Assert */
            Assert.True(statusResult == UserStatus.Existing, "Existing user should return status as existing");
        }
    }
}