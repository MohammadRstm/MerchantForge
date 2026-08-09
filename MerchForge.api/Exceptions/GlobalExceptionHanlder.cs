using MerchForge.api.DTOs.Error;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Exceptions.Base;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace MerchForge.api.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
         HttpContext httpContext,
         Exception exception,
         CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");

            if (exception is AppException appException)
            {
                await HandleApplicationException(
                    httpContext,
                    appException,
                    cancellationToken);

                return true;
            }


            await HandleUnexpectedException(
             httpContext,
             cancellationToken);

            return true;
        }
        private static async Task HandleApplicationException(
            HttpContext httpContext,
            AppException exception,
            CancellationToken cancellationToken)
        {
            var response = new ApiErrorResponse
            {
                Type = exception.Type,
                Code = exception.Code,
                Message = exception.Message,
                TraceId = httpContext.TraceIdentifier
            };

            httpContext.Response.StatusCode =
                GetStatusCode(exception.Type);

            await httpContext.Response.WriteAsJsonAsync(
                response,
                cancellationToken);
        }

        private static async Task HandleUnexpectedException(
           HttpContext httpContext,
           CancellationToken cancellationToken)
        {
            var response = new ApiErrorResponse
            {
                Type = ErrorType.Unexpected,
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred.",
                TraceId = httpContext.TraceIdentifier
            };

            httpContext.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(
                response,
                cancellationToken);
        }

        private static int GetStatusCode(ErrorType type)
        {
            return type switch
            {
                ErrorType.Validation =>
                    StatusCodes.Status400BadRequest,

                ErrorType.Authentication =>
                    StatusCodes.Status401Unauthorized,

                ErrorType.Authorization =>
                    StatusCodes.Status403Forbidden,

                ErrorType.NotFound =>
                    StatusCodes.Status404NotFound,

                ErrorType.Conflict =>
                    StatusCodes.Status409Conflict,

                ErrorType.Unexpected =>
                    StatusCodes.Status500InternalServerError,

                _ => StatusCodes.Status500InternalServerError
            };
        }



        private static string GetTitle(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest =>
                    "Bad Request",

                StatusCodes.Status401Unauthorized =>
                    "Unauthorized",

                StatusCodes.Status403Forbidden =>
                    "Forbidden",

                StatusCodes.Status404NotFound =>
                    "Not Found",

                StatusCodes.Status409Conflict =>
                    "Conflict",

                _ => "Internal Server Error"
            };
        }

        private static string GetDetail(
            Exception exception,
            int statusCode)
        {
            return statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message;
        }
    }
}
