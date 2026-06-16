using System.Collections.Generic;

namespace GestionQ.Web.Models
{
    public class ManageRolePermissionsViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<RolePermissionViewModel> Permissions { get; set; } = new List<RolePermissionViewModel>();
    }

    public class RolePermissionViewModel
    {
        public string PermissionValue { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
