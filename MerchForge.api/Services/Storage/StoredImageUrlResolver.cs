using System.Diagnostics.CodeAnalysis;
using MerchForge.api.Configurations;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Storage
{
    public class StoredImageUrlResolver : IStoredImageUrlResolver
    {
        protected readonly string PublicBaseUrl;

        public StoredImageUrlResolver(IOptions<R2Options> r2Options)
        {
            PublicBaseUrl = r2Options.Value.PublicBaseUrl.TrimEnd('/');
        }

        [return: NotNullIfNotNull(nameof(storedValue))]
        public string? ToPublicUrl(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return storedValue;
            }

            // Images written before the move to object storage are still files under
            // wwwroot and are still served by the API itself, so their stored path is
            // already what the client needs.
            if (IsLegacyLocalPath(storedValue))
            {
                return storedValue;
            }

            // Already absolute. Nothing writes this today, but staying idempotent means
            // a projection that accidentally resolves twice produces the right answer
            // instead of a doubled origin.
            if (storedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || storedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return storedValue;
            }

            return $"{PublicBaseUrl}/{storedValue}";
        }

        public bool IsLegacyLocalPath([NotNullWhen(true)] string? storedValue)
        {
            return storedValue is not null && storedValue.StartsWith('/');
        }
    }
}
