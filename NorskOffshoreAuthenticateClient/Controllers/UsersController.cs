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
        private IUserService _usersService;

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

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            UserItem todo = new UserItem();
            var signedInUser = HttpContext.User.GetDisplayName();
            try
            {
                List<string> result = (await _usersService.GetAllGraphUsersAsync()).ToList();

                //move signed in user to top of the list so it will be selected on Create ToDo item page
                result.Remove(signedInUser);
                result.Insert(0, signedInUser);

                TempData["UsersDropDown"] = result
                .Select(u => new SelectListItem
                {
                    Text = u
                }).ToList();
                TempData["TenantId"] = HttpContext.User.GetTenantId();
                TempData["AssignedBy"] = signedInUser;
                return View(todo);
            }
            catch (WebApiMsalUiRequiredException ex)
            {
                return Redirect(ex.Message);
            }
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Username,Email,TenantId")] UserItem user)
        {
            await _usersService.AddAsync(user);
            return RedirectToAction("Index");
        }

        // GET: Users/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            UserItem user = await this._usersService.GetAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind("Id,Username,Email,TenantId")] UserItem user)
        {
            await _usersService.EditAsync(user);
            return RedirectToAction("Index");
        }

        // GET: Users/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            UserItem todo = await this._usersService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, [Bind("Id,Username,Email")] UserItem user)
        {
            await _usersService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}