namespace MerchForge.api.Services.Common;

// Resolves the acting platform user's id from the current request's JWT claims,
// so services that need to attribute an action (e.g. audit logging) don't have
// to thread an actingUserId parameter through every call site - the controller
// layer already relies on the same claim (ClaimTypes.NameIdentifier) elsewhere.
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
