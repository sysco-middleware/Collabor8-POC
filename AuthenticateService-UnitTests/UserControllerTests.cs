using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using NOA.Common.Constants;
using NorskOffshoreAuthenticateBackend.Controllers;
using NorskOffshoreAuthenticateBackend.Models;
using NOA.Common.Service;
using Xunit.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Moq;
using System.Linq.Expressions;
using Microsoft.Graph.Models;
using Microsoft.CodeAnalysis.Options;
using NOA.Common.Service.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthenticateService_IntegrationTests
{
    public class UserControllerTests
    {
        private UsersController UsersController { get; set; }
        private string userMail_existing = "existing_tom@corporation.org";
        private string userMail_missing = "missing_jonas@corporation.org";
        public UserControllerTests()
        {
            /* Arrange */
            var provider = Startup.StartupContainer.GetServiceProvider();

            var op = provider.GetRequiredService<IOptions<UsersConnectionModel>>();
            var ca = (IHttpContextAccessor)provider.GetService(typeof(IHttpContextAccessor));
            var ia = new Mock<IAuthenticationService>();

            ia.Setup(x => x.GetTokenForUserAsync(It.IsAny<string[]>(), It.IsAny<string>())).ReturnsAsync("123Token");

            var gp = new Mock<IGraphServiceProxy>();

            gp.Setup(x => x.GetGraphApiUser($"mail eq '{userMail_existing}'")).ReturnsAsync(new User()
            {
                EmployeeId = "1",
                UserType = "",
                Mail = userMail_existing,
                UserPrincipalName = userMail_missing,
            });

            //gp.Setup(x => x.GetGraphApiUser($"mail eq '{userMail_missing}'")).ReturnsAsync(null);

            UsersController = new UsersController(op, gp.Object, ca, ia.Object, new Mock<ILogger<UsersController>>().Object);
        }

        [Fact]
        public async Task WhenAuthenticating_ExistingUser_ThenAuthenticationWorks()
        {
            /* Arrange */
            //Set user to authenticate
            

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await UsersController.CanAuthenticateUser(userMail_existing);

            var authenticationResult = controllerResult.Value;

            /* Assert */
            Assert.True(authenticationResult, "Existing user should be able to authenticate");
        }

        [Fact]
        public async Task WhenAuthenticating_NonExistingUser_ThenAuthenticationFails()
        {
            /* Arrange */
            //Set user to authenticate


            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await UsersController.CanAuthenticateUser(userMail_missing);

            var authenticationResult = controllerResult.Value;

            /* Assert */
            Assert.False(authenticationResult, "Missing user should not be able to authenticate");
        }

        [Fact]
        public async Task WhenQueryingUserStatus_OnNonExistingUser_ThenStatusSaysMissing()
        {
            /* Arrange */
            //Set user to authenticate

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await UsersController.GetUserStatus(userMail_missing);

            var statusResult = controllerResult.Value;

            /* Assert */
            Assert.True(statusResult == UserStatus.Missing, "Missing user should return status as missing");
        }

        [Fact]
        public async Task WhenQueryingUserStatus_OnExistingUser_ThenStatusSaysExisting()
        {
            /* Arrange */
            //Set user to authenticate

            /* Act */
            //Do authentication based on e-mail address
            var controllerResult = await UsersController.GetUserStatus(userMail_existing);

            var statusResult = controllerResult.Value;

            /* Assert */
            Assert.True(statusResult == UserStatus.Existing, "Existing user should return status as existing");
        }
    }
}