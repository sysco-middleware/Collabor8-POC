using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NorskOffshoreAuthenticateService.Models;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.Graph.Models;
using NOA.Common.Service;

namespace NorskOffshoreAuthenticateService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly string[] _graphScopes;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationService _authService;
        private readonly string _userTenantId;

        private const string _usersReadScope = "ToDoList.Read";
        private const string _usersReadWriteScope = "ToDoList.ReadWrite";
        private const string _usersReadAllPermission = "ToDoList.Read.All";
        private const string _usersReadWriteAllPermission = "ToDoList.ReadWrite.All";

        public UsersController(
            ITokenAcquisition tokenAcquisition, 
            IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor,
            GraphServiceClient graphServiceClient,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            IAuthenticationService authService)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
            _httpContextAccessor = httpContextAccessor;

            this._graphServiceClient = graphServiceClient; 
            if (this._graphServiceClient == null) 
                throw new NullReferenceException("The GraphServiceClient has not been added to the services collection during the ConfigureServices()");

            this._consentHandler = consentHandler;
            if (this._consentHandler == null) 
                throw new NullReferenceException("The MicrosoftIdentityConsentAndConditionalAccessHandler has not been added to the services collection during the ConfigureServices()");

            _userTenantId = _httpContextAccessor.HttpContext?.User.GetTenantId();
            _authService = authService;
        }

        [HttpPost("authenticateuser")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<bool>> AuthenticateUser(string userMail)
        {
            var filter =
                $"mail eq '{userMail}'";
            var user = await GetGraphApiUser(filter);
            if (user != null)
            {
                var scopes = new string[] { _usersReadScope, _usersReadWriteScope };
                var token = await _authService.GetTokenForUserAsync(scopes, user.UserPrincipalName);
                return !String.IsNullOrEmpty(token);
            }
            return false;
        }

        [HttpGet("getuserstatus")]
        public async Task<ActionResult<UserStatus>> GetUserStatus(string userMail)
        {
            var filter =
                $"mail eq '{userMail}'";
            var user = await GetGraphApiUser(filter);
            if (user != null)
            {
                return UserStatus.Existing;
            }
            return UserStatus.Missing;
        }

        [HttpGet("getloggedingraphuser")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<User>> GetLoggedInGraphUser()
        {
            try
            {
                var filter =
                    $"accountEnabled eq true and id eq '{_httpContextAccessor.HttpContext?.User.GetObjectId()}'";
                var user = await GetGraphApiUser(filter);
                return user;
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync(
                    "An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode +
                    "\n" + ex.Message);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                await HttpContext.Response.WriteAsync(
                    "An Web challenge error occurred, code:\n" + ex.MsalUiRequiredException.StatusCode +
                    "\n" + ex.MsalUiRequiredException.Classification +
                    "\n" + ex.Message);
            }

            return null;
        }

        [HttpGet("getallgraphusers")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<IEnumerable<string>>> GetAllGraphUsers()
        {
            try
            {
                List<string> users = await GetAllGraphApiUsers();
                return users;
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }

            return null;
        }

        private async Task<User> GetGraphApiUser(string filter)
        {
            // we use MSAL.NET to get a token to call the API On Behalf Of the current user
            try
            {
                // Call the Graph API and retrieve the user's profile.
                var users =
                    await CallGraphWithCAEFallback(
                        async () =>
                        {
                            try
                            {
                                return await _graphServiceClient.Users.GetAsync(r =>
                                    {
                                        r.QueryParameters.Filter = filter;
                                        r.QueryParameters.Select = new string[]
                                        {
                                            "id", "userPrincipalName", "displayName", 
                                            "givenName", "accountEnabled", "mail",
                                            "mobilePhone", "country", "streetAddress", 
                                            "photo"
                                        };

                                    }
                                );
                            }
                            catch(Exception ex)
                            {
                                throw;
                            }
                        }
                    );

                if (users?.Value != null)
                {
                    return users.Value?.FirstOrDefault();
                }

                throw new Exception();
            }
            catch (MsalUiRequiredException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(_graphScopes, ex);
                throw;
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<string>> GetAllGraphApiUsers()
        {
            // We use MSAL.NET to get a token to call the API On Behalf Of the current user
            try
            {
                // Call the Graph API and retrieve the user's profile.
                var users =
                await CallGraphWithCAEFallback(
                    async () =>
                    {
                        return await _graphServiceClient.Users.GetAsync(r =>
                                                              {
                                                                  r.QueryParameters.Filter = "accountEnabled eq true";
                                                                  r.QueryParameters.Select = new string[] { "id", "userPrincipalName" };
                                                              }
                                                              );

                    }
                );

                if (users != null)
                {
                    return users.Value.Select(x => x.UserPrincipalName).ToList();
                }
                throw new Exception("Got null from Microsoft Graph Api");
            }
            catch (MsalUiRequiredException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(_graphScopes, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Calls a Microsoft Graph API, but wraps and handle a CAE exception, if thrown
        /// </summary>
        /// <typeparam name="T">The type of the object to return from MS Graph call</typeparam>
        /// <param name="graphAPIMethod">The graph API method to call.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unknown error just occurred. Message: {ex.Message}</exception>
        /// <autogeneratedoc />
        private async Task<T> CallGraphWithCAEFallback<T>(Func<Task<T>> graphAPIMethod)
        {
            try
            {
                return await graphAPIMethod();
            }
            catch (ServiceException ex) when (ex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    // Get challenge from response of Graph API
                    var claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(ex.ResponseHeaders);

                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                }
                catch (Exception ex2)
                {
                    _consentHandler.HandleException(ex2);
                }

                return default;
            }
        }


    }
}
