using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using NOA.Common.Service;
using NOA.Common.Service.Model;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;

namespace NorskOffshoreAuthenticateClient.Controllers
{
    [AuthorizeForScopes(ScopeKeySection = "Users:NorskOffshoreAuthenticateServiceScope")]
    public class UsersController : Controller
    {
        private readonly IUserService _usersService;
        private readonly ILogger<UsersController> _logger;
        private readonly CustomLogOptions _customLogOptions;

        public UsersController(
            IUserService usersService, 
            ILogger<UsersController> logger, 
            IOptions<CustomLogOptions> customLogOptions)
        {
            _usersService = usersService;
            _logger = logger;
            _customLogOptions = customLogOptions.Value;
        }

        // GET: Users
        public async Task<ActionResult> Index()
        {
            try
            {
                TempData["SignedInUser"] = User.GetDisplayName();
                var listWithUser = new List<UserItem>() { await _usersService.GetLoggedInUser() };

                return View(listWithUser);
            } catch (Exception ex)
            {
                LogException(ex);
                throw;
;           }
        }

        // GET: VerifyUser
        public async Task<ActionResult> VerifyUser(string emailAddress)
        {
            try
            {
                if (String.IsNullOrEmpty(emailAddress))
                {
                    ViewData["ErrorToView"] = "Parameter 'emailAddress' was empty. Try again?";
                    return View();
                }

                var status = await _usersService.GetUserStatus(emailAddress);
                ViewData["UserStatus"] = status;
                ViewData["EmailAddress"] = emailAddress;
                return View();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;                
            }
        }

        // POST: CanAuthenticateUser
        public async Task<ActionResult> CanAuthenticateUser(string emailAddress)
        {
            try { 
                if (String.IsNullOrEmpty(emailAddress))
                {
                    ViewData["ErrorToView"] = "Parameter 'emailAddress' was empty. Try again?";
                    return View();
                }

                var status = await _usersService.CanAuthenticateUser(emailAddress);
                ViewData["IsAuthenticated"] = status;
                ViewData["EmailAddress"] = emailAddress;

                return View();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;                
            }
        }

        public async Task<ActionResult> RemoveUserFromGroup(string emailAddress, string groupId)
        {
            try
            {
                var userListModeldata = await _usersService.GetAllUsers();

                if (String.IsNullOrEmpty(emailAddress))
                {
                    ViewData["ErrorToView"] = "Parameter 'emailAddress' was empty. Try again?";
                } else if (String.IsNullOrEmpty(groupId))
                {
                    ViewData["ErrorToView"] = "Parameter 'groupId' was empty. Try again?";

                } else
                {
                    var status = await _usersService.RemoveFromGroup(emailAddress, groupId);
                    ViewData["IsRemovedFromAccessGroupResult"] = status.ToString();
                }

                return View(userListModeldata);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<ActionResult> AddUserToGroup(string emailAddress, string groupId)
        {
            try
            {
                if (String.IsNullOrEmpty(emailAddress))
                {
                    ViewData["ErrorToView"] = "Parameter 'emailAddress' was empty. Try again?";
                    return View();
                }

                if (String.IsNullOrEmpty(groupId))
                {
                    ViewData["ErrorToView"] = "Parameter 'groupId' was empty. Try again?";
                    return View();
                }

                var status = await _usersService.AddToGroup(emailAddress, groupId);

                ViewData["EmailAddress"] = emailAddress;
                ViewData["IsAddedToAccessGroupResult"] = status.ToString();

                return View();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<ActionResult> InviteUser(string emailAddress)
        {
            try
            {
                if (String.IsNullOrEmpty(emailAddress))
                {
                    ViewData["ErrorToView"] = "Parameter 'emailAddress' was empty. Try again?";
                    return View();
                }

                var isInvited = await _usersService.InviteUser(emailAddress);

                ViewData["EmailAddress"] = emailAddress;
                ViewData["IsInvited"] = isInvited.InviteSuccess;
                ViewData["IsAddedToAccessGroup"] = isInvited.AddGroupSuccess;
                ViewData["IsAddedToAccessGroupStatus"] = isInvited.AddToGroupStatus.ToString();

                return View();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private void LogException(Exception ex)
        {
            var message = $"Caught exception of type '{ex.GetType()}' with the message: '{ex.Message + ex.InnerException}'";
            _logger.LogError(message);
            DoCustomLogging(ViewData["ErrorToView"], message);
        }

        private object DoCustomLogging(object logDestination, string logMessage)
        {
            if(_customLogOptions.DoCustomLogForFrontend)
            {
                logDestination += logMessage;
            }
            return logDestination;
        }
    }
}