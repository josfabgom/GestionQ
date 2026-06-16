using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace GestionQ.Web.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null)
            {
                return Task.CompletedTask;
            }

            var permissionssClaim = context.User.Claims.Where(x => x.Type == "Permission" &&
                                                                x.Value == requirement.Permission &&
                                                                x.Issuer == "LOCAL AUTHORITY");

            if (permissionssClaim.Any())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // In ASP.NET Core Identity, role claims are mapped automatically to user claims 
            // if configured correctly, but just in case, we also check if there's any claim 
            // directly with the Permission value (sometimes they lack the specific Issuer).
            var anyPermissionClaim = context.User.Claims.Where(x => x.Type == "Permission" && x.Value == requirement.Permission);
            if (anyPermissionClaim.Any())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
