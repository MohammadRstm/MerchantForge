using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class UsersQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    public SystemRole? SystemRole { get; set; }

    public BusinessRole? BusinessRole { get; set; }

    public bool? HasActiveSession { get; set; }

    /// <summary>null = all, false = active accounts, true = disabled accounts.</summary>
    public bool? IsDisabled { get; set; }

    public UserSortField SortBy { get; set; } = UserSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
