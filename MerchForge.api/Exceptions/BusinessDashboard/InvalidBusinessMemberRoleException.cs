using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    /// <summary>
    /// Raised when a team member is created with a role an owner may not assign.
    ///
    /// In practice that means Owner: a business has exactly one, established at
    /// registration, and letting an owner mint another through the team form would be
    /// a privilege escalation rather than a team change.
    /// </summary>
    public class InvalidBusinessMemberRoleException : AppException
    {
        public InvalidBusinessMemberRoleException() : base(
            Enums.ErrorType.Validation,
            "INVALID_BUSINESS_MEMBER_ROLE",
            "A team member can only be an Admin or a Member.")
        {
        }
    }
}
