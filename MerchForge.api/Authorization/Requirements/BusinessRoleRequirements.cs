using MerchForge.api.Enums;
using Microsoft.AspNetCore.Authorization;


namespace MerchForge.api.Authorization.Requirements
{
    public class BusinessRoleRequirements : IAuthorizationRequirement
    {
        public IReadOnlyCollection<BusinessRole> AllowedRoles { get; }

        public BusinessRoleRequirements(
         params BusinessRole[] allowedRoles)
        {
            AllowedRoles = allowedRoles;
        }
    }
}