using FitCore.BLL.Exceptions;
using System.Net;
using System.Text.Json;


namespace FitCore.API.Middlewares
{
    public class GlobalExceptionMiddleware(RequestDelegate _next, ILogger<GlobalExceptionMiddleware> _logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Un-Expected Error occur!");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message = "Un-Expected Error occur, please try again later";
            string title = "Internal Server Error";
            IEnumerable<string>? validationErrors = null;

            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized; // 401
                    title = "Unauthorized";
                    message = exception.Message;
                    break;

                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound; // 404
                    title = "Not Found";
                    message = exception.Message;
                    break;
                case BusinessRuleException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    title = "Bad Request";
                    message = exception.Message;
                    break;
                case ValidationException valEx:
                    statusCode = (int)HttpStatusCode.BadRequest; // 400
                    title = "Validation Error";
                    message = valEx.Message;
                    validationErrors = valEx.Errors;
                    break;
                case DbUpdateFailedException:
                    statusCode = (int)HttpStatusCode.Conflict; // 409
                    title = "Db Update Failed";
                    message = exception.Message;
                    break;
                case ArgumentNullException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    title = "Null Values";
                    message = exception.Message;
                    break;


            }

            context.Response.StatusCode = statusCode;

            object errorResponse;

            if (validationErrors != null && validationErrors.Any())
            {
                errorResponse = new
                {
                    Title = title,
                    StatusCode = statusCode,
                    Message = message,
                    Errors = validationErrors
                };
            }
            else
            {
                errorResponse = new
                {
                    Title = title,
                    StatusCode = statusCode,
                    Message = message
                };
            }

            var result = JsonSerializer.Serialize(errorResponse);
            return context.Response.WriteAsync(result);
        }
    }
}
