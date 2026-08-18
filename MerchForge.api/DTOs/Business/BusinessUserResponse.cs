using MerchForge.api.Enums;
using MerchForge.api.Models;

public class BusinessUserResponse
{
    public Business Business { get; set; } = null!;
    public BusinessRole BusinessRole { get; set; };
}