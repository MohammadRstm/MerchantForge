namespace MerchForge.api.RateLimiting;

/// <summary>
/// Partition-key selection for every named rate-limit policy registered in
/// Program.cs. Kept as small, pure functions of HttpContext (rather than inline
/// lambdas inside AddRateLimiter) so the actual boundary each policy enforces —
/// which is the part most likely to hide a bug — can be unit tested directly
/// against a constructed HttpContext, without needing a full request pipeline.
/// </summary>
public static class RateLimitPartitions
{
    /// <summary>
    /// Pre-authentication endpoints (login, signup, refresh, the one-time
    /// SuperAdmin bootstrap) have no authenticated identity yet to partition by,
    /// so the client's own IP is the only available boundary.
    /// </summary>
    public static string GetClientIpPartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// AI endpoints are always routed under
    /// api/businesses/{businessId:guid}/dashboard/..., and BusinessOwner
    /// authorization has already confirmed the caller belongs to that exact
    /// business before this ever runs — so the route's businessId is a real,
    /// per-tenant boundary, not client-supplied trust. Partitioning here (not
    /// globally, not per IP) is what stops one business's burst from throttling
    /// every other business sharing this deployment.
    /// </summary>
    public static string GetBusinessPartitionKey(HttpContext httpContext) =>
        httpContext.Request.RouteValues.TryGetValue("businessId", out var value) && value is not null
            ? value.ToString() ?? "unknown"
            : "unknown";

    /// <summary>
    /// The public storefront API takes businessId from the query string, not the
    /// route (see StorefrontController's own doc comment on why) - same
    /// per-business partitioning goal as GetBusinessPartitionKey, just read from
    /// a different place.
    /// </summary>
    public static string GetStorefrontBusinessPartitionKey(HttpContext httpContext) =>
        httpContext.Request.Query.TryGetValue("businessId", out var value) && value.Count > 0
            ? value[0] ?? "unknown"
            : "unknown";
}
