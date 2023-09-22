using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using NorskOffshoreAuthenticateClient.Models;
using NorskOffshoreAuthenticateClient.Services;
using NorskOffshoreAuthenticateClient.Utils;

namespace NorskOffshoreAuthenticateClient.Controllers
{
    [AuthorizeForScopes(ScopeKeySection = "Users:NorskOffshoreAuthenticateServiceScope")]
    public class UsersController : Controller
    {
        private readonly IUserService _usersService;

        public UsersController(IUserService usersService)
        {
            _usersService = usersService;
        }

        // GET: Users
        public async Task<ActionResult> Index()
        {
            TempData["SignedInUser"] = User.GetDisplayName();
            var listWithUser = new List<UserItem>() { await _usersService.GetLoggedInUser() };
            return View(listWithUser);
        }

        // GET: VerifyUser
        public async Task<ActionResult> VerifyUser(string emailAddress)
        {
            if (String.IsNullOrEmpty(emailAddress))
            {
                return View();
            }

            var status = await _usersService.GetUserStatus(emailAddress);
            ViewData["UserStatus"] = status;
            ViewData["EmailAddress"] = emailAddress;
            return View();
        }

        // POST: CanAuthenticateUser
        public async Task<ActionResult> CanAuthenticateUser(string emailAddress)
        {
            if (String.IsNullOrEmpty(emailAddress))
            {
                return View();
            }

            var status = await _usersService.CanAuthenticateUser(emailAddress);
            ViewData["IsAuthenticated"] = status;
            ViewData["EmailAddress"] = emailAddress;

            return View();
        }
    }
}