namespace MerchForge.api.DTOs.Common;

/// <summary>A null day means "not set" — distinct from a day explicitly marked Closed.</summary>
public class BusinessHoursDto
{
    public BusinessHoursDayDto? Monday { get; set; }

    public BusinessHoursDayDto? Tuesday { get; set; }

    public BusinessHoursDayDto? Wednesday { get; set; }

    public BusinessHoursDayDto? Thursday { get; set; }

    public BusinessHoursDayDto? Friday { get; set; }

    public BusinessHoursDayDto? Saturday { get; set; }

    public BusinessHoursDayDto? Sunday { get; set; }
}
