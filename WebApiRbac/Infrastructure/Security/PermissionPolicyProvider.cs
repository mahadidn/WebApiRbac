using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebApiRbac.Infrastructure.Security
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {

        }

        // fungsi ini otomatis terpanggil setiap kali .NET melihat atribut [Authorize(Policy = "...")] di controller
        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // cek, apakah policy ini sudah ada bawaan dari .NET
            var policy = await base.GetPolicyAsync(policyName);

            if(policy == null)
            {
                // jika ga ada, buatkan aturan baru secara dinamis
                // suruh sistem mengecek "PermissionRequirement" dengan nama policy tersebut
                policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
            }

            return policy;
        }

    }
}
