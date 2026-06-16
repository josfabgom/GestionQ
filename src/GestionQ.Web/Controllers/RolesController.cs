using GestionQ.Domain.Constants;
using GestionQ.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Roles.View)]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var model = roles.Select(r => new RoleViewModel
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty
            }).ToList();
            return View(model);
        }

        [Authorize(Policy = Permissions.Roles.Create)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Roles.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var role = new IdentityRole { Name = model.Name };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [Authorize(Policy = Permissions.Roles.Edit)]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            var allPermissions = Permissions.GenerateAllPermissions();

            var model = new ManageRolePermissionsViewModel
            {
                RoleId = roleId,
                RoleName = role.Name ?? string.Empty
            };

            foreach (var permission in allPermissions)
            {
                var parts = permission.Split('.');
                var rawModule = parts.Length > 1 ? parts[1] : "General";
                
                var displayName = permission.Replace("Permissions.", "");
                
                // Nombres de Módulos
                var moduleName = rawModule;
                moduleName = moduleName.Replace("Products", "Productos");
                moduleName = moduleName.Replace("Customers", "Clientes");
                moduleName = moduleName.Replace("Sales", "Ventas");
                moduleName = moduleName.Replace("Purchases", "Compras");
                moduleName = moduleName.Replace("CashRegisters", "Cajas");
                moduleName = moduleName.Replace("Config", "Configuración");
                moduleName = moduleName.Replace("Users", "Usuarios");
                moduleName = moduleName.Replace("Roles", "Roles");
                moduleName = moduleName.Replace("ElectronicInvoices", "Facturación Electrónica");

                // Acciones para mostrar
                var actionName = parts.Length > 2 ? parts[2] : "";
                actionName = actionName.Replace("View", "Ver");
                actionName = actionName.Replace("Create", "Crear");
                actionName = actionName.Replace("Edit", "Editar");
                actionName = actionName.Replace("Delete", "Eliminar");
                actionName = actionName.Replace("Manage", "Administrar");
                actionName = actionName.Replace("Open", "Abrir");
                actionName = actionName.Replace("Close", "Cerrar");
                actionName = actionName.Replace("Movement", "Movimientos");

                displayName = $"{moduleName} ({actionName})";

                model.Permissions.Add(new RolePermissionViewModel
                {
                    PermissionValue = permission,
                    DisplayName = displayName,
                    Module = moduleName,
                    IsSelected = roleClaims.Any(c => c.Type == "Permission" && c.Value == permission)
                });
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Roles.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagePermissions(ManageRolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = claims.Where(c => c.Type == "Permission").ToList();
            
            // Remove all existing permission claims
            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // Add selected permissions
            var selectedPermissions = model.Permissions.Where(p => p.IsSelected).ToList();
            foreach (var permission in selectedPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.PermissionValue));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Roles.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null && role.Name != "Admin") // Prevent deleting Admin
            {
                await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
