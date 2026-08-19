namespace MerchForge.api.DTOs.Common;

public abstract class PagedQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
