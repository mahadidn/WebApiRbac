using Microsoft.AspNetCore.Authorization;

namespace WebApiRbac.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }

    }
}
