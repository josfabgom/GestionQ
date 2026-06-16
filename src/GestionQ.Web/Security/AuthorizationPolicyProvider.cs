using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace GestionQ.Web.Security
{
    public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;

        public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
            _options = options.Value;
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if policy exists natively
            var policy = await base.GetPolicyAsync(policyName);

            if (policy == null)
            {
                // Dynamic policy for Permissions.***
                policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();

                // Cache the policy so it doesn't need to be recreated
                _options.AddPolicy(policyName, policy);
            }

            return policy;
        }
    }
}
