using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class UsersQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    public SystemRole? SystemRole { get; set; }

    public UserSortField SortBy { get; set; } = UserSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
