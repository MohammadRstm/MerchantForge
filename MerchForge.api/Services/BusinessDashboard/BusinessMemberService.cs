using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Invitation.interfaces;
using Microsoft.AspNetCore.Identity;

namespace MerchForge.api.Services.BusinessDashboard
{
    public class BusinessMemberService : IBusinessMemberService
    {
        private readonly IBusinessDashboardRepository _businessDashboardRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IInvitationService _invitationService;

        public BusinessMemberService(
            IBusinessDashboardRepository businessDashboardRepository,
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            IInvitationService invitationService)
        {
            _businessDashboardRepository = businessDashboardRepository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _invitationService = invitationService;
        }

        public async Task<CreateBusinessMemberResponse> CreateMemberAsync(
            Guid businessId,
            CreateBusinessMemberRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            // The route's businessId is only proven to belong to the caller, not to
            // exist — a deleted business would otherwise produce an orphan membership.
            var business = await _businessDashboardRepository.GetBusinessSummaryAsync(
                businessId,
                cancellationToken);

            if (business is null)
            {
                throw new BusinessNotFoundException();
            }

            // Owner is rejected by the validator, but re-checked here so the rule
            // holds for any caller of this service, not only the HTTP endpoint.
            if (request.Role is not (BusinessRole.Admin or BusinessRole.Member))
            {
                throw new InvalidBusinessMemberRoleException();
            }

            var email = request.Email.Trim();

            // Checked up front so a duplicate is a named conflict rather than the
            // unique index on users.Email surfacing as a 500.
            if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
            {
                throw new EmailAlreadyExistsException();
            }

            var systemRoleId = await _userRepository.GetSystemRoleId(SystemRole.User, cancellationToken);
            var businessRoleId = await _userRepository.GetBusinessRoleId(request.Role, cancellationToken);

            var now = DateTime.UtcNow;

            var member = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                SystemRoleId = systemRoleId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // No usable password yet - unattached to any account, generated purely so
            // the stored hash is well-formed and a login attempt fails the ordinary
            // "wrong password" way rather than erroring on a malformed hash. The
            // invitation below is what actually lets this member in, by overwriting
            // this hash with one only they ever know.
            member.PasswordHash = _passwordHasher.HashPassword(member, PasswordGenerator.Generate());

            var membership = new BusinessUser
            {
                UserId = member.Id,
                BusinessId = businessId,
                RoleId = businessRoleId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _businessDashboardRepository.CreateMemberAsync(member, membership, cancellationToken);

            await _invitationService.CreateBusinessMemberInvitationAsync(
                businessId, business!.Value.Name, request, createdByUserId, cancellationToken);

            return new CreateBusinessMemberResponse
            {
                UserId = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                Role = request.Role.ToString(),
                JoinedAt = membership.CreatedAt,
            };
        }
    }
}
