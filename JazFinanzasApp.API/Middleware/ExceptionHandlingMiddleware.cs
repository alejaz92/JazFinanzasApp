using JazFinanzasApp.API.Business.DTOs.Error;
using JazFinanzasApp.API.Business.Exceptions;
using System.Net;
using System.Text.Json;

namespace JazFinanzasApp.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, error) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                UnauthorizedDomainException => (HttpStatusCode.Forbidden, "Forbidden"),
                BusinessRuleException => (HttpStatusCode.BadRequest, "Bad Request"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            var response = new ErrorResponseDTO
            {
                StatusCode = (int)statusCode,
                Error = error,
                Message = statusCode == HttpStatusCode.InternalServerError
                    ? $"[DEBUG] {exception.GetType().Name}: {exception.Message}"
                    : exception.Message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
