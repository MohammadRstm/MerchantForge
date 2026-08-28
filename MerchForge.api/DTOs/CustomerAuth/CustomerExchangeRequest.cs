namespace MerchForge.api.DTOs.CustomerAuth;

public class CustomerExchangeRequest
{
    public string Code { get; set; } = string.Empty;

    /// <summary>Must match the ReturnUrl the code was minted for, exactly.</summary>
    public string ReturnUrl { get; set; } = string.Empty;
}
