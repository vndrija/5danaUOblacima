using System.Net;
using System.Text.Json;
using API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

            var (statusCode, error) = exception switch
            {
                NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
                BadRequestException badRequest => (HttpStatusCode.BadRequest, badRequest.Message),
                ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message),
                UnauthorizedAccessException unauthorized => (HttpStatusCode.Forbidden, unauthorized.Message),
                ArgumentException argument => (HttpStatusCode.BadRequest, argument.Message),
                InvalidOperationException invalidOp => (HttpStatusCode.BadRequest, invalidOp.Message),
                _ => (HttpStatusCode.InternalServerError, "An error occurred while processing your request.")
            };

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";

            var response = new
            {
                error = error
            };

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(response),
                cancellationToken);

            return true;
        }
    }
}
