namespace MerchForge.api.Exceptions.Base
{
    public class AppException : Exception
    {
        protected AppException(string message)
        : base(message)
        {
        }
    }
}
