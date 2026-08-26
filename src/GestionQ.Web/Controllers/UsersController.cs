using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Users.View)]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                var pinClaim = claims.FirstOrDefault(c => c.Type == "UserPin");
                var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");

                userRolesViewModel.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    FullName = fullNameClaim?.Value,
                    Roles = string.Join(", ", roles),
                    Pin = pinClaim?.Value
                });
            }

            return View(userRolesViewModel);
        }

        [Authorize(Policy = Permissions.Users.Create)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.UserName };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                        if (roleExists) await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    if (!string.IsNullOrEmpty(model.Pin))
                    {
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("UserPin", model.Pin));
                    }
                    if (!string.IsNullOrEmpty(model.FullName))
                    {
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", model.FullName));
                    }
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        // Simplest Edit: Just change Role
        [Authorize(Policy = Permissions.Users.Edit)]
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var pinClaim = claims.FirstOrDefault(c => c.Type == "UserPin");
            var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FullName = fullNameClaim?.Value,
                CurrentRole = userRoles.FirstOrDefault(),
                Pin = pinClaim?.Value
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditUserRoleViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles);

            if (!string.IsNullOrEmpty(model.NewRole))
            {
                await _userManager.AddToRoleAsync(user, model.NewRole);
            }

            var claims = await _userManager.GetClaimsAsync(user);
            
            var oldPinClaim = claims.FirstOrDefault(c => c.Type == "UserPin");
            if (oldPinClaim != null) await _userManager.RemoveClaimAsync(user, oldPinClaim);
            
            if (!string.IsNullOrEmpty(model.Pin))
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("UserPin", model.Pin));
            }

            var oldFullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
            if (oldFullNameClaim != null) await _userManager.RemoveClaimAsync(user, oldFullNameClaim);
            
            if (!string.IsNullOrEmpty(model.FullName))
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", model.FullName));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Edit)]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Usuario no encontrado" });

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                return Json(new { success = false, message = "La contraseña debe tener al menos 6 caracteres" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Roles { get; set; }
        public string? Pin { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Nombre del Cajero/Vendedor (Opcional)")]
        public string? FullName { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? Role { get; set; }
        
        [StringLength(4, MinimumLength = 4)]
        public string? Pin { get; set; }
    }

    public class EditUserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? CurrentRole { get; set; }
        public string? NewRole { get; set; }
        
        [StringLength(4, MinimumLength = 4)]
        public string? Pin { get; set; }
    }
}
