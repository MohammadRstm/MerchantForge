using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Error
{
    public class ApiErrorResponse
    {
        public ErrorType Type { get; init; }

        public string Code { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public Dictionary<string, string[]>? Errors { get; init; }

        public string? TraceId { get; init; }
    }
}
