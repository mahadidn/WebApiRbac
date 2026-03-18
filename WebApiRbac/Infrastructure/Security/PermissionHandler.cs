using Microsoft.AspNetCore.Authorization;

namespace WebApiRbac.Infrastructure.Security
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // 1. Cek apakah di dalam KTP (Claims) user saat ini, 
            // ada tipe "permission" yang nilainya SAMA PERSIS dengan requirement yang diminta?
            var hasPermission = context.User.HasClaim(c =>
                c.Type == "permission" &&
                c.Value == requirement.Permission
            );

            // jika punya, succeed
            if (hasPermission)
            {
                context.Succeed(requirement);
            }

            // jika ada context.Succeed maka akan succeed, jika tidak ada maka akan forbidden
            return Task.CompletedTask;

        }
        

    }
}
