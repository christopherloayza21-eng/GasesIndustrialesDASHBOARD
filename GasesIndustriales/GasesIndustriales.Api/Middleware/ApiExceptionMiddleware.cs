using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GasesIndustriales.Api.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
                await EscribirRespuestaError(context, ex);
            }
        }

        private static async Task EscribirRespuestaError(HttpContext context, Exception ex)
        {
            var statusCode = ex is DbUpdateException
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError;

            var response = new ApiErrorResponse
            {
                Status = (int)statusCode,
                Title = statusCode == HttpStatusCode.BadRequest
                    ? "No se pudo guardar la operación."
                    : "Ocurrió un error interno.",
                Detail = ex.InnerException?.Message ?? ex.Message,
                Path = context.Request.Path
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.Status;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public class ApiErrorResponse
    {
        public int Status { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
    }
}
