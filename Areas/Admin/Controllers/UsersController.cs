using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Models;

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var viewModels = new List<UserListViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                viewModels.Add(new UserListViewModel { Id = u.Id, Email = u.Email!, FullName = u.FullName, Roles = roles.ToList(), IsLockedOut = await _userManager.IsLockedOutAsync(u) });
            }
            return View(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (await _userManager.IsLockedOutAsync(user)) await _userManager.SetLockoutEndDateAsync(user, null);
            else await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            return RedirectToAction("Index");
        }
    }
}