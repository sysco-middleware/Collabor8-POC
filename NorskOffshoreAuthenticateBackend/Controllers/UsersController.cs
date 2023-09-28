using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NorskOffshoreAuthenticateBackend.Models;
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
using Microsoft.Extensions.Options;
using NOA.Common.Service.Model;
using Microsoft.Graph.Models.ODataErrors;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace NorskOffshoreAuthenticateBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationService _authService;
        private readonly IGraphServiceProxy _graphServiceProxy;
        private readonly UsersConnectionModel _usersConnectionModel;
        private readonly ILogger<UsersController> _logger;

        private const string _usersReadScope = "ToDoList.Read";
        private const string _usersReadWriteScope = "ToDoList.ReadWrite";
        private const string _usersReadAllPermission = "ToDoList.Read.All";
        private const string _usersReadWriteAllPermission = "ToDoList.ReadWrite.All";

        public UsersController(
            IOptions<UsersConnectionModel> usersConnectionModel,
            IGraphServiceProxy graphServiceProxy,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationService authService,
            ILogger<UsersController> logger)
        {
            _graphServiceProxy = graphServiceProxy;
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
            _usersConnectionModel = usersConnectionModel.Value;
            _logger = logger;
        }



        [HttpPost("AddToGroup")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<AddToGroupStatus> AddToGroup(string userMail, string groupId)
        {
            if (String.IsNullOrEmpty(userMail) || String.IsNullOrEmpty(groupId))
            {
                return AddToGroupStatus.MissingParameters;
            }

            try {
                var groupMembers = await _graphServiceProxy.GetGroupMembers(groupId);
                var userAdded = await _graphServiceProxy.GetGraphApiUser($"mail eq '{userMail}'");
                if (!groupMembers.Exists(x => x.Id == userAdded.Id))
                {
                    var invitationResult = await _graphServiceProxy.AddUserToGroup(
                        userMail,
                        groupId);
                    return invitationResult ? AddToGroupStatus.Success : AddToGroupStatus.Failed;
                }
                return AddToGroupStatus.AlreadyMember;
            }
            catch (Exception e)
            {
                _logger.LogError($"Caught exception of type '{e.GetType()}' with message: '{e.Message + e.InnerException}'");
            }

            return AddToGroupStatus.Failed;            
        }

        [HttpPost("InviteUser")]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _usersReadScope, _usersReadWriteScope },
            AcceptedAppPermission = new string[] { _usersReadAllPermission, _usersReadWriteAllPermission })]
        public async Task<ActionResult<InviteUserResult>> InviteUser(string userMail, string redirectUrl)
        {
            if (String.IsNullOrEmpty(userMail))
            {
                return new InviteUserResult() {
                    InviteSuccess = false,
                    AddGroupSuccess = false
                };
            }

            var invitation = await _graphServiceProxy.InviteUser(
                userMail,
                redirectUrl);

            var addUserToGroupresult = true;
            try
            {
                if (invitation != null && invitation.Status.ToLower() != "error")
                {
                    Thread.Sleep(2000);
                    var groupMembers = await _graphServiceProxy.GetGroupMembers(_usersConnectionModel.AccessGroupId);
                    var userAdded = await _graphServiceProxy.GetGraphApiUser($"mail eq '{userMail}'");
                    if (!groupMembers.Exists(x => x.Id == userAdded.Id))
                    {
                        await _graphServiceProxy.AddUserToGroup(userMail, _usersConnectionModel.AccessGroupId);
                    }
                    
                } else
                {
                    addUserToGroupresult = false;
                }
            } catch (Exception e) {
                addUserToGroupresult = false;
                _logger.LogError($"Caught exception of type '{e.GetType()}' with message: '{e.Message + e.InnerException}'");
            }

            return new InviteUserResult()
            {
                AddGroupSuccess = addUserToGroupresult,
                InviteSuccess = (invitation?.Status.ToLower() ?? "error") != "error"
            };
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
                var scopes = new string[] { _usersConnectionModel.NorskOffshoreAuthenticateServiceScope };
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
            catch (MicrosoftIdentityWebChallengeUserException e)
            {
                _logger.LogError($"Caught exception of type '{e.GetType()}' with message: '{e.Message + e.InnerException}'");
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                await HttpContext.Response.WriteAsync(
                    "An Web challenge error occurred, code:\n" + e.MsalUiRequiredException.StatusCode +
                    "\n" + e.MsalUiRequiredException.Classification +
                    "\n" + e.Message);
            } catch (ODataError e)
            {
                _logger.LogError($"Caught exception of type '{e.GetType()}' with message: '{e.Message + e.InnerException}'");
                throw;
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
                _logger.LogError($"Caught exception of type '{ex.GetType()}' with message: '{ex.Message + ex.InnerException}'");
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }

            return null;
        }      
    }
}
