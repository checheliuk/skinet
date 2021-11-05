using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Middlewere
{
    public class ExceptionMiddlewere
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddlewere> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddlewere(RequestDelegate next, ILogger<ExceptionMiddlewere> logger, IHostEnvironment env)
        {
            _env = env;
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = (true)
                    ? new ApiException((int)HttpStatusCode.InternalServerError, ex.Message, ex.StackTrace.ToString())
                    : new ApiResponse((int)HttpStatusCode.InternalServerError);

                var option = new JsonSerializerOptions 
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json =  JsonSerializer.Serialize(response, response.GetType(), option);
                await context.Response.WriteAsync(json);
            }
        }
    }
}