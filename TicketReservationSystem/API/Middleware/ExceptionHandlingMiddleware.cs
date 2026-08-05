using Microsoft.AspNetCore.Mvc;

namespace TicketReservationSystem.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private const string GenericErrorMessage = "An unexpected error occurred";

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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception during request processing");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = GenericErrorMessage,
                };

                await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
            }
        }
    }
}
