namespace MerchForge.api.DTOs.Contact;

/// <summary>
/// An enquiry from the public contact form. Anonymous by nature: the whole
/// point is that owners cannot register themselves, so the only way in is to
/// ask - which means nothing here can be trusted and every field is validated
/// and length-capped before it reaches an email body.
/// </summary>
public class ContactEnquiryRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
