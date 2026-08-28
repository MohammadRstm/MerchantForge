namespace MerchForge.api.DTOs.Common;

/// <summary>Open/Close are "HH:mm" 24-hour strings, ignored when Closed is true.</summary>
public class BusinessHoursDayDto
{
    public bool Closed { get; set; }

    public string? Open { get; set; }

    public string? Close { get; set; }
}
