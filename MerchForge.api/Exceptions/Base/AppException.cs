using MerchForge.api.Enums;

namespace MerchForge.api.Exceptions.Base
{
    public class AppException : Exception
    {
        public ErrorType Type { get; }

        public string Code { get; }
        protected AppException(
            ErrorType type,
            string code,
            string message)
        : base(message)
        {
            Type = type;
            Code = code;
        }
    }
}
