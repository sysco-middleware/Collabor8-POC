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
using NOA.Common.Constants;
using NOA.Common.Service;

namespace NorskOffshoreAuthenticateService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationService _authService;
        private readonly IGraphServiceProxy _graphServiceProxy;

        private const string _usersReadScope = "ToDoList.Read";
        private const string _usersReadWriteScope = "ToDoList.ReadWrite";
        private const string _usersReadAllPermission = "ToDoList.Read.All";
        private const string _usersReadWriteAllPermission = "ToDoList.ReadWrite.All";

        public UsersController(
            IGraphServiceProxy graphServiceProxy,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationService authService)
        {
            _graphServiceProxy = graphServiceProxy;
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
        }

        [HttpPost("CanAuthenticateUser")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<bool>> CanAuthenticateUser(string userMail)
        {
            if(String.IsNullOrEmpty(userMail))
            {
                return false;
            }

            var filter = $"mail eq '{userMail}'";
            var user = await _graphServiceProxy.GetGraphApiUser(filter);
            if (user != null && !String.IsNullOrEmpty(user.UserPrincipalName))
            {
                var scopes = new string[] { _usersReadScope, _usersReadWriteScope };
                var token = await _authService.GetTokenForUserAsync(scopes, user.UserPrincipalName);
                return !String.IsNullOrEmpty(token);
            }
            return false;
        }

        [HttpGet("getuserstatus")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<UserStatus>> GetUserStatus(string userMail)
        {
            if (String.IsNullOrEmpty(userMail))
            {
                return UserStatus.Missing;
            }

            var filter =
                $"mail eq '{userMail}'";
            var user = await _graphServiceProxy.GetGraphApiUser(filter);
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
                var user = await _graphServiceProxy.GetGraphApiUser(filter);
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
                List<string> users = await _graphServiceProxy.GetAllGraphApiUsers();
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

        


    }
}
