using System;
using System.Collections.Generic;
using System.Linq;
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
                HttpRequest request = context.Request;
                request.EnableBuffering();
                var bodyStream = new StreamReader(request.Body);
                var requestBodyText = await bodyStream.ReadToEndAsync();
                request.Body.Position = 0;
                var jsonRequest = JsonSerializer.Serialize(new
                {
                    Date = DateTime.Now,
                    Host = request.Host,
                    ContentType = request.ContentType,
                    Method = request.Method,
                    Path = request.Path,
                    QueryString = request.QueryString,
                    Headers = request.Headers,
                    Body = requestBodyText
                });

                await _next(context);

                if (_settings.EnableResponseLogging)
                {
                    HttpResponse response = context.Response;
                    response.Body.Seek(0, SeekOrigin.Begin);    
                    var responseBodyText = await new StreamReader(response.Body).ReadToEndAsync();
                    response.Body.Seek(0, SeekOrigin.Begin);

                    var jsonResponse = JsonSerializer.Serialize(new
                    {
                        Date = DateTime.Now,
                        StatusCodes = response.StatusCode,
                        Body = responseBodyText
                    });
                }
            }
        }


    }
}
