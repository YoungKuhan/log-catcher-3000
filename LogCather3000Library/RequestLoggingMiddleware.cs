using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogCather3000Library
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly LoggingSettings _settings;

        public RequestLoggingMiddleware(RequestDelegate next, 
            ILogger<RequestLoggingMiddleware> logger,
            IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _settings = config.GetSection("Logging").Get<LoggingSettings>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (_settings.EnableRequestLogging)
            {
                await LogRequest(context);
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context); //Przekazujemy context do kolejnych elementów pipeline

            if (_settings.EnableResponseLogging)
            {
                await LogResponse(context, responseBody);
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task LogRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            string jsonRequest = JsonSerializer.Serialize(new
            {
                Date = DateTime.Now,
                Host = request.Host.Value,          
                ContentType = request.ContentType,
                Method = request.Method,
                Path = request.Path.Value,         
                QueryString = request.QueryString.Value.ToString(), 
                Headers = request.Headers,
                Body = body
            });

            _logger.LogInformation("Incoming Request: " + jsonRequest);
            Console.WriteLine(jsonRequest);
        }

        private async Task LogResponse(HttpContext context, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBody, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            string jsonResponse = JsonSerializer.Serialize(new
            {
                Date = DateTime.Now,
                StatusCodes = context.Response.StatusCode,
                Body = body,
                ContentType = context.Response.ContentType
            });
            _logger.LogInformation("Response: " + jsonResponse);
        }


    }
}
