using MerchForge.api.Authorization;
using MerchForge.api.DTOs.Invitations;
using MerchForge.api.Services.Invitation.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MerchForge.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(
            IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost("business-owner")]
        [Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
        public async Task<ActionResult<InvitationResponse>> CreateBusinessOwnerInvitation(
           [FromBody] CreateBusinessOwnerInvitationRequest request,
           CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized();
            }

            var response =
                await _invitationService
                    .CreateBusinessOwnerInvitationAsync(
                        request,
                        parsedUserId,
                        cancellationToken);

            return Ok(response);
        }
    }
}
